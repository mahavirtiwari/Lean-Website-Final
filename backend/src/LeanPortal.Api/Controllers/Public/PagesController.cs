using LeanPortal.Application.Contracts;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Public;

/// <summary>Serves published CMS pages to the public site.</summary>
[Route("pages")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class PagesController(ApplicationDbContext db) : ApiControllerBase(db)
{
    /// <summary>
    /// Fetch a published page by slug. Slugs are hierarchical, so the catch-all route matches
    /// values such as <c>about-scheme/financial-assistance</c>.
    /// </summary>
    [HttpGet("{**slug}")]
    [ProducesResponseType<PageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageDto>> GetBySlug(string slug, CancellationToken ct)
    {
        slug = (slug ?? string.Empty).Trim('/');
        if (slug.Length == 0) slug = "home";

        var page = await Db.Pages
            .AsNoTracking()
            .Include(p => p.Blocks)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PublishStatus.Published, ct);

        if (page is null) return NotFoundProblem("Page");

        return Ok(page.ToDto(
            await BuildBreadcrumbsAsync(page, ct),
            await BuildSiblingsAsync(page, ct)));
    }

    /// <summary>Titles and summaries of the published children of a page.</summary>
    [HttpGet("{id:int}/children")]
    [ProducesResponseType<IReadOnlyList<PageSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PageSummaryDto>>> GetChildren(int id, CancellationToken ct)
    {
        var children = await Db.Pages.AsNoTracking()
            .Where(p => p.ParentId == id && p.Status == PublishStatus.Published)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .ToListAsync(ct);

        return Ok(children.Select(MappingExtensions.ToSummaryDto).ToList());
    }

    /// <summary>Walks up the parent chain to build the breadcrumb trail shown under the page banner.</summary>
    private async Task<IReadOnlyList<BreadcrumbDto>> BuildBreadcrumbsAsync(Page page, CancellationToken ct)
    {
        var trail = new List<BreadcrumbDto>();
        var currentId = page.ParentId;

        // Depth is bounded to avoid an infinite loop if data is ever cyclic.
        for (var depth = 0; currentId is not null && depth < 8; depth++)
        {
            var parent = await Db.Pages.AsNoTracking()
                .Where(p => p.Id == currentId)
                .Select(p => new { p.Slug, p.Title, p.ShortTitle, p.ParentId, p.Status })
                .FirstOrDefaultAsync(ct);

            if (parent is null) break;

            trail.Insert(0, new BreadcrumbDto(
                parent.ShortTitle ?? parent.Title,
                parent.Status == PublishStatus.Published ? "/" + parent.Slug : null));

            currentId = parent.ParentId;
        }

        trail.Insert(0, new BreadcrumbDto("Home", "/"));
        trail.Add(new BreadcrumbDto(page.ShortTitle ?? page.Title, null));
        return trail;
    }

    /// <summary>
    /// Sibling pages under the same parent - this is the left-hand section navigation on inner pages.
    /// A top-level page shows its own children instead.
    /// </summary>
    private async Task<IReadOnlyList<PageSummaryDto>> BuildSiblingsAsync(Page page, CancellationToken ct)
    {
        if (!page.ShowSidebarNav) return [];

        var parentId = page.ParentId ?? page.Id;

        var siblings = await Db.Pages.AsNoTracking()
            .Where(p => p.ParentId == parentId && p.Status == PublishStatus.Published)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .ToListAsync(ct);

        return siblings.Select(MappingExtensions.ToSummaryDto).ToList();
    }
}
