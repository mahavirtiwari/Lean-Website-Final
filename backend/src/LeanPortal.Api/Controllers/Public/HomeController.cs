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

/// <summary>
/// Assembles the entire landing page in a single response, so the Angular home route renders
/// without a waterfall of requests.
/// </summary>
[Route("home")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class HomeController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<HomeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeDto>> Get(CancellationToken ct)
    {
        var page = await Db.Pages.AsNoTracking()
            .Include(p => p.Blocks)
            .FirstOrDefaultAsync(p => p.Slug == "home" && p.Status == PublishStatus.Published, ct);

        if (page is null) return NotFoundProblem("The home page");

        var now = DateTimeOffset.UtcNow;

        var banners = await Db.Banners.AsNoTracking()
            .Where(b => b.IsActive
                        && (b.StartsAt == null || b.StartsAt <= now)
                        && (b.EndsAt == null || b.EndsAt >= now))
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);

        var statistics = await Db.Statistics.AsNoTracking()
            .Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToListAsync(ct);

        var components = await Db.SchemeComponents.AsNoTracking()
            .Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct);

        var levels = await Db.SchemeLevels.AsNoTracking()
            .Where(l => l.IsActive).OrderBy(l => l.SortOrder).ToListAsync(ct);

        var portals = await Db.LoginPortals.AsNoTracking()
            .Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync(ct);

        var documents = await Db.Documents.AsNoTracking()
            .Where(d => d.Status == PublishStatus.Published && d.IsFeatured)
            .OrderBy(d => d.SortOrder)
            .Take(6)
            .ToListAsync(ct);

        var publishedPosts = Db.Posts.AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published
                        && (p.ExpiresAt == null || p.ExpiresAt > now));

        var latestPosts = await publishedPosts
            .OrderByDescending(p => p.PublishedAt)
            .Take(4)
            .ToListAsync(ct);

        var tickerPosts = await publishedPosts
            .Where(p => p.ShowInTicker)
            .OrderByDescending(p => p.PublishedAt)
            .Take(8)
            .ToListAsync(ct);

        var testimonials = await Db.Testimonials.AsNoTracking()
            .Where(t => t.Status == PublishStatus.Published)
            .OrderBy(t => t.SortOrder)
            .Take(6)
            .ToListAsync(ct);

        // The showcase draws from every published album rather than one, so the newest
        // work is on the landing page without an editor having to curate a second list.
        var galleryItems = await Db.GalleryImages.AsNoTracking()
            .Include(i => i.Album)
            .Where(i => i.IsActive && i.Album!.Status == PublishStatus.Published)
            .OrderBy(i => i.Album!.SortOrder)
            .ThenByDescending(i => i.Album!.EventDate)
            .ThenBy(i => i.SortOrder)
            .ToListAsync(ct);

        var galleryPhotos = galleryItems.Where(i => string.IsNullOrWhiteSpace(i.VideoUrl)).Take(12).ToList();
        var galleryVideos = galleryItems.Where(i => !string.IsNullOrWhiteSpace(i.VideoUrl)).Take(12).ToList();

        // Everyone the scheme works with except the Useful Links tiles, which are a
        // list of other services rather than the bodies delivering this one.
        var partners = await Db.Partners.AsNoTracking()
            .Where(p => p.IsActive && p.Type != PartnerType.UsefulLink)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        var usefulLinks = await Db.Partners.AsNoTracking()
            .Where(p => p.IsActive && p.Type == PartnerType.UsefulLink)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        var benefits = await Db.Benefits.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);

        return Ok(new HomeDto(
            page.ToDto(),
            banners.Select(MappingExtensions.ToDto).ToList(),
            statistics.Select(MappingExtensions.ToDto).ToList(),
            components.Select(MappingExtensions.ToDto).ToList(),
            levels.Select(MappingExtensions.ToDto).ToList(),
            portals.Select(MappingExtensions.ToDto).ToList(),
            documents.Select(MappingExtensions.ToDto).ToList(),
            latestPosts.Select(MappingExtensions.ToSummaryDto).ToList(),
            tickerPosts.Select(MappingExtensions.ToSummaryDto).ToList(),
            testimonials.Select(MappingExtensions.ToDto).ToList(),
            usefulLinks.Select(MappingExtensions.ToDto).ToList(),
            galleryPhotos.Select(MappingExtensions.ToDto).ToList(),
            galleryVideos.Select(MappingExtensions.ToDto).ToList(),
            partners.Select(MappingExtensions.ToDto).ToList(),
            benefits.Select(MappingExtensions.ToDto).ToList()));
    }
}
