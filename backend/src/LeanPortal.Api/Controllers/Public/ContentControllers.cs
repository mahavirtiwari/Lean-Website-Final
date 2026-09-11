using LeanPortal.Application.Common;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Public;

// -------------------------------------------------------------- incentives ----

/// <summary>
/// The incentives listed under Benefits / Incentives, one category at a time.
///
/// The filter values come back with the results rather than being fixed in the
/// client, so the page never offers a level or a state that would return nothing -
/// and a state that has just notified an incentive appears in the dropdown as soon
/// as the entry is published.
/// </summary>
[Route("incentives")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class IncentivesController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<IncentiveListDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IncentiveListDto>> Get(
        [FromQuery] IncentiveCategory category,
        [FromQuery] string? level,
        [FromQuery] string? state,
        CancellationToken ct = default)
    {
        var all = await Db.Incentives.AsNoTracking()
            .Where(i => i.IsActive && i.Category == category)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .ToListAsync(ct);

        // Offered before filtering, so choosing a level never empties the dropdown
        // that would let the reader choose a different one.
        var levels = all.Where(i => !string.IsNullOrWhiteSpace(i.Level))
            .Select(i => i.Level!).Distinct().OrderBy(x => x).ToList();

        var states = all.Where(i => !string.IsNullOrWhiteSpace(i.State))
            .Select(i => i.State!).Distinct().OrderBy(x => x).ToList();

        var shown = all.AsEnumerable();

        // An entry with no level applies at every level, so it survives the filter.
        if (!string.IsNullOrWhiteSpace(level))
            shown = shown.Where(i => string.IsNullOrWhiteSpace(i.Level)
                                     || string.Equals(i.Level, level, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(state))
            shown = shown.Where(i => string.IsNullOrWhiteSpace(i.State)
                                     || string.Equals(i.State, state, StringComparison.OrdinalIgnoreCase));

        return Ok(new IncentiveListDto(
            shown.Select(MappingExtensions.ToDto).ToList(), levels, states, all.Count));
    }
}

// ------------------------------------------------------------------- posts ----

/// <summary>News, announcements, circulars, tenders and success stories.</summary>
[Route("posts")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class PostsController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<PostSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PostSummaryDto>>> GetAll(
        [FromQuery] PagedQuery query, [FromQuery] PostType? type, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var source = Db.Posts.AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published && (p.ExpiresAt == null || p.ExpiresAt > now));

        if (type is not null)
            source = source.Where(p => p.Type == type);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                                       || EF.Functions.Like(p.Excerpt ?? string.Empty, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.PublishedAt)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<PostSummaryDto>.Create(
            items.Select(MappingExtensions.ToSummaryDto).ToList(), query.Page, query.PageSize, total));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType<PostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetBySlug(string slug, CancellationToken ct)
    {
        var post = await Db.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PublishStatus.Published, ct);

        if (post is null) return NotFoundProblem("Post");

        var related = await Db.Posts.AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published && p.Type == post.Type && p.Id != post.Id)
            .OrderByDescending(p => p.PublishedAt)
            .Take(3)
            .ToListAsync(ct);

        // Fire-and-forget style view counter: a failure here must not fail the read.
        await Db.Posts.Where(p => p.Id == post.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

        return Ok(post.ToDto(related.Select(MappingExtensions.ToSummaryDto).ToList()));
    }
}

// --------------------------------------------------------------- documents ----

/// <summary>Downloadable scheme guidelines, brochures, circulars and formats.</summary>
[Route("documents")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class DocumentsController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetAll(
        [FromQuery] DocumentCategory? category, [FromQuery] string? search, CancellationToken ct)
    {
        var source = Db.Documents.AsNoTracking().Where(d => d.Status == PublishStatus.Published);

        if (category is not null)
            source = source.Where(d => d.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(d => EF.Functions.Like(d.Title, $"%{term}%")
                                       || EF.Functions.Like(d.Description ?? string.Empty, $"%{term}%"));
        }

        var items = await source
            .OrderBy(d => d.Category).ThenBy(d => d.SortOrder).ThenByDescending(d => d.DocumentDate)
            .ToListAsync(ct);

        return Ok(items.Select(MappingExtensions.ToDto).ToList());
    }

    /// <summary>Records the download and redirects to the stored file.</summary>
    [HttpGet("{id:int}/download")]
    [OutputCache(NoStore = true)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Download(int id, CancellationToken ct)
    {
        var document = await Db.Documents.AsNoTracking()
            .Where(d => d.Id == id && d.Status == PublishStatus.Published)
            .Select(d => new { d.Id, d.FileUrl })
            .FirstOrDefaultAsync(ct);

        if (document is null) return NotFoundProblem("Document");

        await Db.Documents.Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.DownloadCount, d => d.DownloadCount + 1), ct);

        return Redirect(document.FileUrl);
    }
}

// -------------------------------------------------------------------- faqs ----

[Route("faqs")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class FaqsController(ApplicationDbContext db) : ApiControllerBase(db)
{
    /// <summary>All published FAQs, grouped by category in display order.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FaqGroupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FaqGroupDto>>> GetAll(
        [FromQuery] string? search, CancellationToken ct)
    {
        var source = Db.Faqs.AsNoTracking().Where(f => f.Status == PublishStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(f => EF.Functions.Like(f.Question, $"%{term}%")
                                       || EF.Functions.Like(f.Answer, $"%{term}%"));
        }

        var items = await source.OrderBy(f => f.SortOrder).ToListAsync(ct);

        var groups = items
            .GroupBy(f => f.Category ?? "General")
            .Select(g => new FaqGroupDto(g.Key, g.Select(MappingExtensions.ToDto).ToList()))
            .ToList();

        return Ok(groups);
    }
}

// ----------------------------------------------------------------- gallery ----

[Route("gallery")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class GalleryController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GalleryAlbumSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GalleryAlbumSummaryDto>>> GetAlbums(CancellationToken ct)
    {
        var albums = await Db.GalleryAlbums.AsNoTracking()
            .Include(a => a.Images)
            .Where(a => a.Status == PublishStatus.Published)
            .OrderBy(a => a.SortOrder).ThenByDescending(a => a.EventDate)
            .ToListAsync(ct);

        return Ok(albums.Select(MappingExtensions.ToSummaryDto).ToList());
    }

    [HttpGet("{slug}")]
    [ProducesResponseType<GalleryAlbumDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GalleryAlbumDto>> GetAlbum(string slug, CancellationToken ct)
    {
        var album = await Db.GalleryAlbums.AsNoTracking()
            .Include(a => a.Images)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.Status == PublishStatus.Published, ct);

        return album is null ? NotFoundProblem("Album") : Ok(album.ToDto());
    }
}

// -------------------------------------------------------------- programmes ----

[Route("programmes")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class ProgrammesController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProgrammeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProgrammeDto>>> GetAll(
        [FromQuery] PagedQuery query,
        [FromQuery] string? state,
        [FromQuery] string? district,
        [FromQuery] string? type,
        [FromQuery] string? agency,
        [FromQuery] ProgrammeStatus? status,
        CancellationToken ct)
    {
        var source = Db.AwarenessProgrammes.AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published);

        if (!string.IsNullOrWhiteSpace(state)) source = source.Where(p => p.State == state);
        if (!string.IsNullOrWhiteSpace(district)) source = source.Where(p => p.District == district);
        if (!string.IsNullOrWhiteSpace(type)) source = source.Where(p => p.ProgrammeType == type);
        if (!string.IsNullOrWhiteSpace(agency)) source = source.Where(p => p.Agency == agency);
        if (status is not null) source = source.Where(p => p.ProgrammeStatus == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                                       || EF.Functions.Like(p.Venue ?? string.Empty, $"%{term}%")
                                       || EF.Functions.Like(p.ProgrammeCode, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(p => p.StartDate)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<ProgrammeDto>.Create(
            items.Select(MappingExtensions.ToDto).ToList(), query.Page, query.PageSize, total));
    }

    /// <summary>Distinct filter values, so the client can populate its dropdowns.</summary>
    [HttpGet("filters")]
    [ProducesResponseType<ProgrammeFiltersDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgrammeFiltersDto>> GetFilters([FromQuery] string? state, CancellationToken ct)
    {
        var source = Db.AwarenessProgrammes.AsNoTracking().Where(p => p.Status == PublishStatus.Published);

        var states = await source.Where(p => p.State != null)
            .Select(p => p.State!).Distinct().OrderBy(s => s).ToListAsync(ct);

        var districtSource = string.IsNullOrWhiteSpace(state) ? source : source.Where(p => p.State == state);
        var districts = await districtSource.Where(p => p.District != null)
            .Select(p => p.District!).Distinct().OrderBy(d => d).ToListAsync(ct);

        var types = await source.Select(p => p.ProgrammeType).Distinct().OrderBy(t => t).ToListAsync(ct);

        var agencies = await source.Where(p => p.Agency != null)
            .Select(p => p.Agency!).Distinct().OrderBy(a => a).ToListAsync(ct);

        return Ok(new ProgrammeFiltersDto(states, districts, types, agencies));
    }
}

// ----------------------------------------------------------- scheme data ----

/// <summary>Scheme reference data used by the About pages and the home page bands.</summary>
[Route("scheme")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class SchemeController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet("levels")]
    [ProducesResponseType<IReadOnlyList<SchemeLevelDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SchemeLevelDto>>> GetLevels(CancellationToken ct) =>
        Ok((await Db.SchemeLevels.AsNoTracking().Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder).ToListAsync(ct))
            .Select(MappingExtensions.ToDto).ToList());

    [HttpGet("components")]
    [ProducesResponseType<IReadOnlyList<SchemeComponentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SchemeComponentDto>>> GetComponents(CancellationToken ct) =>
        Ok((await Db.SchemeComponents.AsNoTracking().Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ToListAsync(ct))
            .Select(MappingExtensions.ToDto).ToList());

    [HttpGet("statistics")]
    [ProducesResponseType<IReadOnlyList<StatisticDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatisticDto>>> GetStatistics(CancellationToken ct) =>
        Ok((await Db.Statistics.AsNoTracking().Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder).ToListAsync(ct))
            .Select(MappingExtensions.ToDto).ToList());

    [HttpGet("login-portals")]
    [ProducesResponseType<IReadOnlyList<LoginPortalDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LoginPortalDto>>> GetLoginPortals(CancellationToken ct) =>
        Ok((await Db.LoginPortals.AsNoTracking().Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder).ToListAsync(ct))
            .Select(MappingExtensions.ToDto).ToList());

    [HttpGet("partners")]
    [ProducesResponseType<IReadOnlyList<PartnerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartnerDto>>> GetPartners(
        [FromQuery] PartnerType? type, CancellationToken ct)
    {
        var source = Db.Partners.AsNoTracking().Where(p => p.IsActive);
        if (type is not null) source = source.Where(p => p.Type == type);

        var items = await source.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(ct);
        return Ok(items.Select(MappingExtensions.ToDto).ToList());
    }

    [HttpGet("testimonials")]
    [ProducesResponseType<IReadOnlyList<TestimonialDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestimonialDto>>> GetTestimonials(CancellationToken ct) =>
        Ok((await Db.Testimonials.AsNoTracking().Where(t => t.Status == PublishStatus.Published)
            .OrderBy(t => t.SortOrder).ToListAsync(ct))
            .Select(MappingExtensions.ToDto).ToList());
}
