using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Renames the implementation levels in content already saved: Basic becomes
    /// Bronze, Intermediate Silver, Advanced Gold.
    ///
    /// Changing the seed data alone would only reach a database seeded from scratch.
    /// The level names run through page bodies, summaries, home-page bands, level
    /// records and success stories on every installation already running, and a
    /// portal that says Bronze in one place and Basic in another is worse than one
    /// that has not been renamed at all.
    ///
    /// The words are replaced only where they name a level. Nothing on this portal
    /// uses them in another sense - that was checked against the content before this
    /// was written - but the longer phrases are still replaced first so "Basic Level"
    /// becomes "Bronze Level" rather than "Bronze level Level".
    ///
    /// Idempotent: once the old words are gone there is nothing left to match, so
    /// this costs one pass and no writes on every subsequent start.
    /// </summary>
    private static async Task RenameSchemeLevelsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        (string From, string To)[] swaps =
        [
            ("Basic Level", "Bronze Level"),
            ("Intermediate Level", "Silver Level"),
            ("Advanced Level", "Gold Level"),
            ("Basic", "Bronze"),
            ("Intermediate", "Silver"),
            ("Advanced", "Gold"),
        ];

        string? Fix(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var result = value;
            foreach (var (from, to) in swaps) result = result.Replace(from, to);
            return result;
        }

        // Two fields renamed together: writes them back and counts 1 when either changed.
        int Apply(Func<(string? A, string? B)> read, Action<(string?, string?)> write)
        {
            var (a, b) = read();
            var (fixedA, fixedB) = (Fix(a), Fix(b));
            if (fixedA == a && fixedB == b) return 0;
            write((fixedA, fixedB));
            return 1;
        }

        var touched = 0;

        // ------------------------------------------------------------ the levels ----
        foreach (var level in await db.SchemeLevels.ToListAsync(ct))
        {
            var name = Fix(level.Name);
            var tagline = Fix(level.Tagline);
            var description = Fix(level.Description);
            var deliverables = Fix(level.Deliverables);

            // The badge used to add something - "Basic Level" carried a Bronze badge.
            // After the rename it repeats the name, and a card reading "Bronze" above
            // "Bronze Level" looks like a mistake, so it goes.
            var badge = level.BadgeLabel;
            if (!string.IsNullOrWhiteSpace(badge) && !string.IsNullOrWhiteSpace(name)
                && name.Contains(badge, StringComparison.OrdinalIgnoreCase))
                badge = null;

            if (name == level.Name && tagline == level.Tagline
                && description == level.Description && deliverables == level.Deliverables
                && badge == level.BadgeLabel)
                continue;

            level.BadgeLabel = badge;
            level.Name = name ?? level.Name;
            level.Tagline = tagline;
            level.Description = description;
            level.Deliverables = deliverables;
            touched++;
        }

        // ------------------------------------------------------------- the pages ----
        foreach (var page in await db.Pages.ToListAsync(ct))
        {
            var body = Fix(page.Body);
            var summary = Fix(page.Summary);
            var meta = Fix(page.MetaDescription);

            if (body == page.Body && summary == page.Summary && meta == page.MetaDescription)
                continue;

            page.Body = body;
            page.Summary = summary;
            page.MetaDescription = meta;
            touched++;
        }

        // ------------------------------------------------- the home page's bands ----
        foreach (var block in await db.PageBlocks.ToListAsync(ct))
        {
            var heading = Fix(block.Heading);
            var sub = Fix(block.SubHeading);
            var body = Fix(block.Body);

            if (heading == block.Heading && sub == block.SubHeading && body == block.Body)
                continue;

            block.Heading = heading;
            block.SubHeading = sub;
            block.Body = body;
            touched++;
        }

        // --------------------------------------------------------- success stories ----
        foreach (var story in await db.Testimonials.ToListAsync(ct))
        {
            var quote = Fix(story.Quote);
            var impact = Fix(story.ImpactHighlight);

            if (quote == story.Quote && impact == story.ImpactHighlight) continue;

            story.Quote = quote ?? story.Quote;
            story.ImpactHighlight = impact;
            touched++;
        }

        // ------------------------------------------------------------- incentives ----
        foreach (var incentive in await db.Incentives.ToListAsync(ct))
        {
            var level = Fix(incentive.Level);
            if (level == incentive.Level) continue;

            incentive.Level = level;
            touched++;
        }

        // -------------------------------------------------- everything else said ----
        // The first pass reached the levels, the pages and the home page's bands, and
        // left the rest: FAQ answers, notices, banners, document titles, the
        // component cards, statistic labels and the quick-action cards still said
        // Basic, Intermediate and Advanced. Only wording is changed here - never a
        // slug, a file address or a statistic's data key, which are addresses and
        // would break if renamed.
        foreach (var faq in await db.Faqs.ToListAsync(ct))
            touched += Apply(() => (faq.Question, faq.Answer), v => (faq.Question, faq.Answer) = (v.Item1 ?? faq.Question, v.Item2 ?? faq.Answer));

        foreach (var post in await db.Posts.ToListAsync(ct))
        {
            var (title, excerpt, body, meta) = (Fix(post.Title), Fix(post.Excerpt), Fix(post.Body), Fix(post.MetaDescription));
            if (title == post.Title && excerpt == post.Excerpt && body == post.Body && meta == post.MetaDescription) continue;
            (post.Title, post.Excerpt, post.Body, post.MetaDescription) = (title ?? post.Title, excerpt, body, meta);
            touched++;
        }

        foreach (var banner in await db.Banners.ToListAsync(ct))
        {
            var (eyebrow, title, highlight, sub, alt) =
                (Fix(banner.Eyebrow), Fix(banner.Title), Fix(banner.HighlightedTitle), Fix(banner.Subtitle), Fix(banner.AltText));
            if (eyebrow == banner.Eyebrow && title == banner.Title && highlight == banner.HighlightedTitle
                && sub == banner.Subtitle && alt == banner.AltText) continue;
            (banner.Eyebrow, banner.Title, banner.HighlightedTitle, banner.Subtitle, banner.AltText) =
                (eyebrow, title ?? banner.Title, highlight, sub, alt);
            touched++;
        }

        foreach (var doc in await db.Documents.ToListAsync(ct))
            touched += Apply(() => (doc.Title, doc.Description), v => (doc.Title, doc.Description) = (v.Item1 ?? doc.Title, v.Item2));

        foreach (var component in await db.SchemeComponents.ToListAsync(ct))
        {
            var (title, shortText, text) = (Fix(component.Title), Fix(component.ShortDescription), Fix(component.Description));
            if (title == component.Title && shortText == component.ShortDescription && text == component.Description) continue;
            (component.Title, component.ShortDescription, component.Description) = (title ?? component.Title, shortText, text);
            touched++;
        }

        foreach (var stat in await db.Statistics.ToListAsync(ct))
        {
            var label = Fix(stat.Label);
            if (label == stat.Label) continue;
            stat.Label = label ?? stat.Label;
            touched++;
        }

        foreach (var portal in await db.LoginPortals.ToListAsync(ct))
            touched += Apply(() => (portal.Title, portal.Description), v => (portal.Title, portal.Description) = (v.Item1 ?? portal.Title, v.Item2));

        // The quick-action cards keep their wording in the band's settings. Only the
        // capitalised words are replaced, so a lower-case address inside the JSON
        // - /lms/basic - is left as it is.
        foreach (var block in await db.PageBlocks.Where(b => b.SettingsJson != null).ToListAsync(ct))
        {
            var json = Fix(block.SettingsJson);
            if (json == block.SettingsJson) continue;
            block.SettingsJson = json;
            touched++;
        }

        if (touched == 0) return;

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Renamed the scheme levels to Bronze / Silver / Gold in {Count} records.", touched);
    }
}
