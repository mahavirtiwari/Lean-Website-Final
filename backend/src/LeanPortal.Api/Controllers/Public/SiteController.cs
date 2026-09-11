using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services;
using LeanPortal.Infrastructure.Services.Branding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Public;

/// <summary>Site-wide chrome: settings, navigation menus and the sitemap.</summary>
[Route("site")]
[EnableRateLimiting("public")]
[OutputCache(PolicyName = "public-content")]
public class SiteController(ApplicationDbContext db, ISiteSettingsProvider settings, VisitorCounter visitors)
    : ApiControllerBase(db)
{
    /// <summary>Public site settings (branding, contact details, social links, feature toggles).</summary>
    [HttpGet("settings")]
    [ProducesResponseType<SiteSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteSettingsDto>> GetSettings(CancellationToken ct)
        => Ok(new SiteSettingsDto(await settings.GetPublicSettingsAsync(ct)));

    /// <summary>
    /// The colour theme chosen in the console, as a stylesheet.
    ///
    /// Linked from the page head rather than applied by script once the settings
    /// have loaded, so the page is drawn in its theme from the first paint instead
    /// of flashing the default colours first. Revalidated on every load with an
    /// ETag, so a new theme shows on the next page view and an unchanged one costs a
    /// 304.
    /// </summary>
    [HttpGet("theme.css")]
    [Produces("text/css")]
    public async Task<IActionResult> GetTheme(CancellationToken ct)
    {
        var css = ThemePalette.Css(
            await settings.GetAsync("theme.preset", ct),
            await settings.GetAsync("theme.customPrimary", ct),
            await settings.GetAsync("theme.customDark", ct));

        var etag = "\"" + Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(css)))[..16] + "\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "no-cache";

        if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);

        return Content(css, "text/css; charset=utf-8");
    }

    /// <summary>Every navigation menu on the site, each as a nested tree.</summary>
    [HttpGet("navigation")]
    [ProducesResponseType<NavigationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NavigationDto>> GetNavigation(CancellationToken ct)
    {
        var items = await Db.MenuItems
            .AsNoTracking()
            .Include(m => m.Page)
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        // Rebuild the parent/child graph in memory so each location renders as a tree.
        var byId = items.ToDictionary(m => m.Id);
        foreach (var item in items)
        {
            item.Children = items.Where(c => c.ParentId == item.Id).OrderBy(c => c.SortOrder).ToList();
        }

        IReadOnlyList<MenuItemDto> Roots(MenuLocation location) => items
            .Where(m => m.Location == location && (m.ParentId is null || !byId.ContainsKey(m.ParentId.Value)))
            .OrderBy(m => m.SortOrder)
            .Select(MappingExtensions.ToDto)
            .ToList();

        return Ok(new NavigationDto(
            Roots(MenuLocation.TopBar),
            Roots(MenuLocation.Main),
            Roots(MenuLocation.Footer),
            Roots(MenuLocation.QuickLinks),
            Roots(MenuLocation.UsefulLinks),
            Roots(MenuLocation.FooterBottom)));
    }

    /// <summary>
    /// Records one visit and returns the running total.
    ///
    /// <para>
    /// Counted in memory and written in batches by <see cref="VisitorCounter"/>, so
    /// a crowd arriving at once does not queue on the one row that holds the total.
    /// The client calls
    /// this once per browser session, so the figure is closer to sessions than to
    /// page views - which is the honest reading of a site visitor counter.
    /// </para>
    /// <para>
    /// Not output-cached: a cached response would return a stale total and, worse,
    /// would stop the increment running at all.
    /// </para>
    /// </summary>
    [HttpPost("visit")]
    [OutputCache(NoStore = true)]
    [ProducesResponseType<VisitorCountDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitorCountDto>> RecordVisit(CancellationToken ct)
    {
        if (!await settings.IsEnabledAsync("feature.visitorCount", ct))
            return Ok(new VisitorCountDto(0, false));

        return Ok(new VisitorCountDto(await visitors.RecordAsync(ct), true));
    }

    /// <summary>Reads the running total without recording a visit.</summary>
    [HttpGet("visitors")]
    [OutputCache(NoStore = true)]
    [ProducesResponseType<VisitorCountDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitorCountDto>> GetVisitorCount(CancellationToken ct)
    {
        if (!await settings.IsEnabledAsync("feature.visitorCount", ct))
            return Ok(new VisitorCountDto(0, false));

        // From the counter, not the settings cache: the cache is held for ten
        // minutes, so it would report a total that visits since have moved on.
        return Ok(new VisitorCountDto(await visitors.CurrentAsync(ct), true));
    }

    /// <summary>A hierarchical index of every published page, used by the sitemap page.</summary>
    [HttpGet("sitemap")]
    [ProducesResponseType<IReadOnlyList<SitemapNodeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SitemapNodeDto>>> GetSitemap(CancellationToken ct)
    {
        var pages = await Db.Pages
            .AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published && p.Slug != "home")
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .Select(p => new { p.Id, p.ParentId, p.Slug, p.Title, p.ShortTitle })
            .ToListAsync(ct);

        SitemapNodeDto Build(int? parentId) => new(
            string.Empty, null,
            pages.Where(p => p.ParentId == parentId)
                .Select(p => new SitemapNodeDto(
                    p.ShortTitle ?? p.Title,
                    "/" + p.Slug,
                    Build(p.Id).Children))
                .ToList());

        return Ok(Build(null).Children);
    }

    /// <summary>
    /// XML sitemap for search engine crawlers.
    ///
    /// Reachable at both paths on purpose. In production the API is a child IIS
    /// application under /api, so a crawler asking the site root for /sitemap.xml
    /// never reaches this controller - the SPA rewrite hands it index.html. The
    /// relative route is the one robots.txt points at; the root route keeps working
    /// when the API is hosted on its own at the root, as it is in development.
    /// </summary>
    [HttpGet("sitemap.xml")]
    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    public async Task<ContentResult> GetSitemapXml(CancellationToken ct)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";

        var pages = await Db.Pages.AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published)
            .Select(p => new { p.Slug, Updated = p.UpdatedAt ?? p.PublishedAt })
            .ToListAsync(ct);

        var posts = await Db.Posts.AsNoTracking()
            .Where(p => p.Status == PublishStatus.Published)
            .Select(p => new { Slug = "media/news/" + p.Slug, Updated = p.UpdatedAt ?? p.PublishedAt })
            .ToListAsync(ct);

        var albums = await Db.GalleryAlbums.AsNoTracking()
            .Where(a => a.Status == PublishStatus.Published)
            .Select(a => new { Slug = "gallery/" + a.Slug, Updated = a.UpdatedAt ?? a.PublishedAt })
            .ToListAsync(ct);

        // Screens of the site's own, not pages in the console - listed so a search
        // engine finds them too. The two integration screens only once they are
        // switched on: a crawler sent to "not available yet" learns nothing useful.
        var integrations = await Db.ExternalIntegrations.AsNoTracking()
            .Where(i => i.Mode != IntegrationMode.Off
                        && (i.Key == "certificate-verification" || i.Key == "certified-units"))
            .Select(i => i.Key == "certificate-verification" ? "verify-certificate" : "certified-units")
            .ToListAsync(ct);

        var screens = new[] { "media/news", "downloads", "faqs", "gallery", "programmes/awareness",
                              "programmes/training", "contact-us", "sitemap" }
            .Concat(integrations)
            .Select(slug => new { Slug = slug, Updated = (DateTimeOffset?)null });

        // A screen that is also a page in the console is listed once, with the page's date.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var all = pages.Concat(posts).Concat(albums).Concat(screens).Where(p => seen.Add(p.Slug));

        var entries = all.Select(p => string.Concat(
            "  <url><loc>", origin, "/", p.Slug == "home" ? string.Empty : p.Slug, "</loc>",
            p.Updated is null ? string.Empty : $"<lastmod>{p.Updated:yyyy-MM-dd}</lastmod>",
            "</url>"));

        var xml = string.Join('\n',
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">",
            string.Join('\n', entries),
            "</urlset>");

        return Content(xml, "application/xml");
    }
}
