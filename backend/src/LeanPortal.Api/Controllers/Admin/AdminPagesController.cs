using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Common;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Admin;

/// <summary>Page management including the editorial workflow and the home page block builder.</summary>
[Route("admin/pages")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public class AdminPagesController(
    ApplicationDbContext db,
    IAuditService audit,
    IContentSanitizer sanitizer) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminPageListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminPageListItemDto>>> GetAll(
        [FromQuery] PagedQuery query, [FromQuery] PublishStatus? status, [FromQuery] int? parentId,
        CancellationToken ct)
    {
        var source = Db.Pages.AsNoTracking().Include(p => p.Parent).AsQueryable();

        if (status is not null) source = source.Where(p => p.Status == status);
        if (parentId is not null) source = source.Where(p => p.ParentId == parentId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                                       || EF.Functions.Like(p.Slug, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<AdminPageListItemDto>.Create(
            items.Select(MappingExtensions.ToListItemDto).ToList(), query.Page, query.PageSize, total));
    }

    /// <summary>The full page tree, for the parent picker and the reorder screen.</summary>
    [HttpGet("tree")]
    [ProducesResponseType<IReadOnlyList<AdminPageListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminPageListItemDto>>> GetTree(CancellationToken ct)
    {
        var pages = await Db.Pages.AsNoTracking()
            .Include(p => p.Parent)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .ToListAsync(ct);

        return Ok(pages.Select(MappingExtensions.ToListItemDto).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPageDto>> GetById(int id, CancellationToken ct)
    {
        var page = await Db.Pages.AsNoTracking()
            .Include(p => p.Blocks)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return page is null ? NotFoundProblem("Page") : Ok(page.ToAdminDto());
    }

    [HttpPost]
    [ProducesResponseType<AdminPageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminPageDto>> Create([FromBody] SavePageRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(request, null, ct) is { } error) return BadRequestProblem(error);

        var page = new Page { Status = PublishStatus.Draft };
        Apply(request, page);

        Db.Pages.Add(page);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Create", nameof(Page), page.Id.ToString(), new { page.Slug, page.Title }, ct);
        return CreatedAtAction(nameof(GetById), new { id = page.Id }, page.ToAdminDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPageDto>> Update(
        int id, [FromBody] SavePageRequest request, CancellationToken ct)
    {
        var page = await Db.Pages.Include(p => p.Blocks).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (page is null) return NotFoundProblem("Page");

        if (await ValidateAsync(request, page, ct) is { } error) return BadRequestProblem(error);

        Apply(request, page);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Update", nameof(Page), page.Id.ToString(), new { page.Slug, page.Title }, ct);
        return Ok(page.ToAdminDto());
    }

    /// <summary>Moves a page through draft, review, published and archived.</summary>
    [HttpPost("{id:int}/status")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var page = await Db.Pages.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (page is null) return NotFoundProblem("Page");

        page.Status = request.Status;

        if (request.Status == PublishStatus.Published)
            page.PublishedAt = request.PublishedAt ?? page.PublishedAt ?? DateTimeOffset.UtcNow;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("ChangeStatus", nameof(Page), id.ToString(), new { request.Status }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var page = await Db.Pages.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (page is null) return NotFoundProblem("Page");

        if (page.Slug == "home")
            return ConflictProblem("The home page cannot be deleted.");

        if (await Db.Pages.AnyAsync(p => p.ParentId == id, ct))
            return ConflictProblem("This page has child pages. Move or delete them first.");

        if (await Db.MenuItems.AnyAsync(m => m.PageId == id, ct))
            return ConflictProblem("This page is referenced by a menu item. Remove the menu item first.");

        page.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(Page), id.ToString(), new { page.Slug }, ct);
        return NoContent();
    }

    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reorder([FromBody] ReorderRequest request, CancellationToken ct)
    {
        var ids = request.Items.Select(i => i.Id).ToList();
        var pages = await Db.Pages.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

        foreach (var item in request.Items)
        {
            if (pages.FirstOrDefault(p => p.Id == item.Id) is { } page)
                page.SortOrder = item.SortOrder;
        }

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("Reorder", nameof(Page), null, request.Items, ct);
        return NoContent();
    }

    // ------------------------------------------------------- page blocks ----

    [HttpGet("{pageId:int}/blocks")]
    [ProducesResponseType<IReadOnlyList<AdminPageBlockDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminPageBlockDto>>> GetBlocks(int pageId, CancellationToken ct)
    {
        var blocks = await Db.PageBlocks.AsNoTracking()
            .Where(b => b.PageId == pageId)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);

        return Ok(blocks.Select(MappingExtensions.ToAdminDto).ToList());
    }

    [HttpPost("{pageId:int}/blocks")]
    [ProducesResponseType<AdminPageBlockDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPageBlockDto>> CreateBlock(
        int pageId, [FromBody] SavePageBlockRequest request, CancellationToken ct)
    {
        if (!await Db.Pages.AnyAsync(p => p.Id == pageId, ct)) return NotFoundProblem("Page");

        var block = new PageBlock { PageId = pageId };
        ApplyBlock(request, block);

        Db.PageBlocks.Add(block);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Create", nameof(PageBlock), block.Id.ToString(), new { pageId, request.Type }, ct);
        return CreatedAtAction(nameof(GetBlocks), new { pageId }, block.ToAdminDto());
    }

    [HttpPut("{pageId:int}/blocks/{blockId:int}")]
    [ProducesResponseType<AdminPageBlockDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPageBlockDto>> UpdateBlock(
        int pageId, int blockId, [FromBody] SavePageBlockRequest request, CancellationToken ct)
    {
        var block = await Db.PageBlocks.FirstOrDefaultAsync(b => b.Id == blockId && b.PageId == pageId, ct);
        if (block is null) return NotFoundProblem("Page block");

        ApplyBlock(request, block);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Update", nameof(PageBlock), blockId.ToString(), new { pageId, request.Type }, ct);
        return Ok(block.ToAdminDto());
    }

    [HttpDelete("{pageId:int}/blocks/{blockId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteBlock(int pageId, int blockId, CancellationToken ct)
    {
        var block = await Db.PageBlocks.FirstOrDefaultAsync(b => b.Id == blockId && b.PageId == pageId, ct);
        if (block is null) return NotFoundProblem("Page block");

        Db.PageBlocks.Remove(block);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(PageBlock), blockId.ToString(), new { pageId }, ct);
        return NoContent();
    }

    [HttpPost("{pageId:int}/blocks/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ReorderBlocks(
        int pageId, [FromBody] ReorderRequest request, CancellationToken ct)
    {
        var ids = request.Items.Select(i => i.Id).ToList();
        var blocks = await Db.PageBlocks.Where(b => b.PageId == pageId && ids.Contains(b.Id)).ToListAsync(ct);

        foreach (var item in request.Items)
        {
            if (blocks.FirstOrDefault(b => b.Id == item.Id) is { } block)
                block.SortOrder = item.SortOrder;
        }

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("Reorder", nameof(PageBlock), pageId.ToString(), request.Items, ct);
        return NoContent();
    }

    // ------------------------------------------------------------ helpers ----

    private void Apply(SavePageRequest r, Page p)
    {
        p.Slug = r.Slug.Trim().Trim('/').ToLowerInvariant();
        p.Title = r.Title.Trim();
        p.ShortTitle = r.ShortTitle?.Trim();
        p.Summary = r.Summary?.Trim();
        p.Body = sanitizer.Sanitize(r.Body);
        p.Template = r.Template;
        p.CustomComponent = r.CustomComponent?.Trim();
        p.ParentId = r.ParentId;
        p.SortOrder = r.SortOrder;
        p.BannerImageUrl = r.BannerImageUrl;
        p.BannerCaption = r.BannerCaption;
        p.ShowInMainMenu = r.ShowInMainMenu;
        p.ShowSidebarNav = r.ShowSidebarNav;
        p.MetaTitle = r.MetaTitle?.Trim();
        p.MetaDescription = r.MetaDescription?.Trim();
        p.MetaKeywords = r.MetaKeywords?.Trim();
        p.OgImageUrl = r.OgImageUrl;
    }

    private void ApplyBlock(SavePageBlockRequest r, PageBlock b)
    {
        b.Type = r.Type;
        b.SortOrder = r.SortOrder;
        b.IsVisible = r.IsVisible;
        b.Eyebrow = r.Eyebrow;
        b.Heading = r.Heading;
        b.SubHeading = r.SubHeading;
        b.Body = sanitizer.Sanitize(r.Body);
        b.ImageUrl = r.ImageUrl;
        b.VideoUrl = r.VideoUrl;
        b.PrimaryLinkText = r.PrimaryLinkText;
        b.PrimaryLinkUrl = r.PrimaryLinkUrl;
        b.SecondaryLinkText = r.SecondaryLinkText;
        b.SecondaryLinkUrl = r.SecondaryLinkUrl;
        b.SettingsJson = r.SettingsJson;
    }

    private async Task<string?> ValidateAsync(SavePageRequest r, Page? existing, CancellationToken ct)
    {
        var slug = r.Slug.Trim().Trim('/').ToLowerInvariant();

        if (await Db.Pages.AnyAsync(p => p.Slug == slug && (existing == null || p.Id != existing.Id), ct))
            return $"The slug '{slug}' is already in use by another page.";

        if (existing is not null && r.ParentId == existing.Id)
            return "A page cannot be its own parent.";

        if (r.ParentId is not null)
        {
            if (!await Db.Pages.AnyAsync(p => p.Id == r.ParentId, ct))
                return "The selected parent page does not exist.";

            // Walk the ancestry to make sure the move does not create a cycle.
            if (existing is not null)
            {
                var ancestorId = r.ParentId;
                for (var depth = 0; ancestorId is not null && depth < 16; depth++)
                {
                    if (ancestorId == existing.Id)
                        return "That parent would create a circular page hierarchy.";

                    ancestorId = await Db.Pages.Where(p => p.Id == ancestorId)
                        .Select(p => p.ParentId).FirstOrDefaultAsync(ct);
                }
            }
        }

        if (r.Template == PageTemplate.Custom && string.IsNullOrWhiteSpace(r.CustomComponent))
            return "A custom template requires a component key.";

        return null;
    }
}
