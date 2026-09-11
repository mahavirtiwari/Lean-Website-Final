using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Common;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Common;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Admin;

/// <summary>
/// Shared list / read / create / update / delete / reorder behaviour for the straightforward admin
/// resources (banners, FAQs, testimonials, scheme levels and so on). Each concrete controller only
/// supplies its query shaping, its DTO projection and how a save request maps onto the entity.
/// </summary>
/// <typeparam name="TEntity">The aggregate being managed.</typeparam>
/// <typeparam name="TDto">The shape returned to the admin client.</typeparam>
/// <typeparam name="TRequest">The shape accepted when creating or updating.</typeparam>
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public abstract class AdminCrudControllerBase<TEntity, TDto, TRequest>(
    ApplicationDbContext db,
    IAuditService audit) : ApiControllerBase(db)
    where TEntity : AuditableEntity, new()
{
    protected IAuditService Audit { get; } = audit;

    /// <summary>Display name used in audit entries and error messages.</summary>
    protected abstract string EntityName { get; }

    protected abstract DbSet<TEntity> Set { get; }

    protected abstract TDto ToDto(TEntity entity);

    /// <summary>Copies the validated request onto the entity. Called for both create and update.</summary>
    protected abstract void Apply(TRequest request, TEntity entity);

    /// <summary>Default ordering for the list endpoint.</summary>
    protected abstract IQueryable<TEntity> ApplyOrder(IQueryable<TEntity> query);

    /// <summary>Free-text search. Override where the resource has searchable text.</summary>
    protected virtual IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, string term) => query;

    /// <summary>Extra includes needed to project the DTO.</summary>
    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query) => query;

    /// <summary>Hook for validation that needs the database (uniqueness, referential checks).</summary>
    protected virtual Task<string?> ValidateAsync(TRequest request, TEntity? existing, CancellationToken ct)
        => Task.FromResult<string?>(null);

    /// <summary>Hook run after a create or update, before the audit entry is written.</summary>
    protected virtual Task AfterSaveAsync(TEntity entity, bool created, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Blocks a delete when other content still references the entity.</summary>
    protected virtual Task<string?> CanDeleteAsync(TEntity entity, CancellationToken ct)
        => Task.FromResult<string?>(null);

    /// <summary>Sets the sort column during a reorder.</summary>
    protected abstract void SetSortOrder(TEntity entity, int sortOrder);

    // ------------------------------------------------------------ endpoints ----

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<PagedResult<TDto>>> GetAll(
        [FromQuery] PagedQuery query, CancellationToken ct)
    {
        var source = ApplyIncludes(Set.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
            source = ApplySearch(source, query.Search.Trim());

        var total = await source.CountAsync(ct);

        var items = await ApplyOrder(source)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<TDto>.Create(items.Select(ToDto).ToList(), query.Page, query.PageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TDto>> GetById(int id, CancellationToken ct)
    {
        var entity = await ApplyIncludes(Set.AsNoTracking()).FirstOrDefaultAsync(e => e.Id == id, ct);
        return entity is null ? NotFoundProblem(EntityName) : Ok(ToDto(entity));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<TDto>> Create([FromBody] TRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(request, null, ct) is { } error) return BadRequestProblem(error);

        var entity = new TEntity();
        Apply(request, entity);

        Set.Add(entity);
        await Db.SaveChangesAsync(ct);
        await AfterSaveAsync(entity, created: true, ct);

        await Audit.LogAsync("Create", EntityName, entity.Id.ToString(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TDto>> Update(int id, [FromBody] TRequest request, CancellationToken ct)
    {
        var entity = await ApplyIncludes(Set).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return NotFoundProblem(EntityName);

        if (await ValidateAsync(request, entity, ct) is { } error) return BadRequestProblem(error);

        Apply(request, entity);
        await Db.SaveChangesAsync(ct);
        await AfterSaveAsync(entity, created: false, ct);

        await Audit.LogAsync("Update", EntityName, entity.Id.ToString(), request, ct);
        return Ok(ToDto(entity));
    }

    /// <summary>Soft-deletes the record so it disappears from the site but remains auditable.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Set.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return NotFoundProblem(EntityName);

        if (await CanDeleteAsync(entity, ct) is { } blocker) return ConflictProblem(blocker);

        entity.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await Audit.LogAsync("Delete", EntityName, id.ToString(), ct: ct);
        return NoContent();
    }

    /// <summary>Persists a drag-and-drop re-order in a single request.</summary>
    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public virtual async Task<ActionResult> Reorder([FromBody] ReorderRequest request, CancellationToken ct)
    {
        var ids = request.Items.Select(i => i.Id).ToList();
        var entities = await Set.Where(e => ids.Contains(e.Id)).ToListAsync(ct);

        foreach (var item in request.Items)
        {
            if (entities.FirstOrDefault(e => e.Id == item.Id) is { } entity)
                SetSortOrder(entity, item.SortOrder);
        }

        await Db.SaveChangesAsync(ct);
        await Audit.LogAsync("Reorder", EntityName, null, request.Items, ct);
        return NoContent();
    }
}
