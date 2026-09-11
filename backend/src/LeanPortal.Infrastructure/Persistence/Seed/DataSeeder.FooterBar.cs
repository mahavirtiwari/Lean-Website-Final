using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>The links GIGW sites carry in the strip at the very bottom of the page.</summary>
    private static readonly string[] BottomBarSlugs = ["policies/disclaimer", "sitemap", "screen-reader-access"];

    /// <summary>
    /// Moves Disclaimer, Sitemap and Screen Reader Access out of the policies
    /// column and into the bottom bar, beside the ownership line - where GIGW sites
    /// put them and where the ministry asked for them.
    ///
    /// Once only: when the bottom bar already has links, it is an editor's to
    /// arrange under Navigation, and this leaves it alone.
    /// </summary>
    private static async Task SeedFooterBarAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.MenuItems.AnyAsync(m => m.Location == MenuLocation.FooterBottom, ct)) return;

        var pageIds = await db.Pages
            .Where(p => BottomBarSlugs.Contains(p.Slug))
            .ToDictionaryAsync(p => p.Id, p => p.Slug, ct);

        var links = await db.MenuItems
            .Where(m => m.Location == MenuLocation.Footer && m.ParentId == null)
            .ToListAsync(ct);

        string? SlugOf(Domain.Entities.MenuItem m) =>
            m.PageId is { } id && pageIds.TryGetValue(id, out var slug) ? slug
            : BottomBarSlugs.FirstOrDefault(s => string.Equals(m.Url?.Trim('/'), s, StringComparison.OrdinalIgnoreCase));

        var moving = links
            .Select(m => (Item: m, Slug: SlugOf(m)))
            .Where(x => x.Slug is not null)
            .OrderBy(x => Array.IndexOf(BottomBarSlugs, x.Slug))
            .ToList();

        if (moving.Count == 0) return;

        var order = 1;
        foreach (var (item, _) in moving)
        {
            item.Location = MenuLocation.FooterBottom;
            item.SortOrder = order++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Moved {Count} links to the footer's bottom bar", moving.Count);
    }
}
