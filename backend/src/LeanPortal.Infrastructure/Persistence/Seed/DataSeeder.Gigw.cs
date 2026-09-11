using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Tops up the pages and links the Guidelines for Indian Government Websites and
    /// Apps (GIGW 3.0) require every government portal to publish.
    ///
    /// This runs as a top-up rather than as part of <c>SeedPagesAsync</c>, which stops
    /// the moment a single page exists: an installation seeded before these pages were
    /// written would otherwise never receive them. Only slugs that are absent are
    /// created, and nothing already in the database is modified - a page an editor has
    /// rewritten stays rewritten, and one they deliberately unpublished stays that way.
    ///
    /// The wording below is the standard departmental text. It is a starting point the
    /// ministry is expected to review and adapt in the CMS, not a legal position this
    /// codebase is asserting on their behalf: the review and archival cycles in
    /// particular name periods that each department sets for itself.
    /// </summary>
    private static async Task SeedGigwComplianceAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // ------------------------------------------------------------------ pages ----
        var existingSlugs = await db.Pages.Select(p => p.Slug).ToListAsync(ct);
        var known = existingSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Page P(string slug, string title, string body, int order, string summary,
               string? shortTitle = null) => new()
        {
            Slug = slug,
            Title = title,
            ShortTitle = shortTitle,
            Summary = summary,
            Body = body,
            Template = PageTemplate.SidebarLeft,
            BannerCaption = "Website policies",
            SortOrder = order,
            Status = PublishStatus.Published,
            PublishedAt = now,
            MetaTitle = title,
            MetaDescription = summary,
            ShowSidebarNav = true
        };

        var required = new List<Page>
        {
            P("policies/content-archival-policy", "Content Archival Policy",
                """
                <p>Content on this portal is archived once it has served the purpose for which it was
                published, so that visitors are not shown material that no longer applies while the record
                of it remains available.</p>
                <p>Announcements, circulars, tenders and event notices are moved out of the active listing
                after their validity or closing date has passed. Scheme guidelines superseded by a revised
                version are retained and marked with the date from which they ceased to apply.</p>
                <p>Archived material is not deleted. It is retained in accordance with the record retention
                rules of the Ministry of Micro, Small and Medium Enterprises and can be requested through
                the contact details published on this portal.</p>
                """, 10,
                "How and when material on this portal is moved out of active listings, and how long it is kept.",
                "Content Archival"),

            P("policies/content-review-policy", "Content Review Policy",
                """
                <p>Every page on this portal has an owner within the scheme division who is responsible for
                confirming that what it says is still correct.</p>
                <p>Content that carries dates, figures or eligibility conditions is reviewed whenever the
                underlying guidelines change and, in any case, at least once a year. Contact details,
                implementing agency information and downloadable formats are checked on the same cycle.</p>
                <p>Where a review finds that content is out of date, it is corrected or withdrawn before the
                review is closed. The date the site was last updated is published in the footer.</p>
                <p>If you find something on this portal that appears to be inaccurate or out of date, please
                tell us through the contact form and it will be checked.</p>
                """, 11,
                "Who is responsible for keeping content correct, and how often it is checked.",
                "Content Review"),

            P("policies/website-monitoring-policy", "Website Monitoring Policy",
                """
                <p>This portal is monitored so that it stays available, quick to load, correct in what it
                links to, and usable by everyone.</p>
                <p>Monitoring covers the quality of the content and its links, the availability of the site
                and the time it takes to load, its behaviour on mobile and desktop browsers, and its
                conformance with the accessibility requirements set out in GIGW and WCAG 2.1 Level AA.</p>
                <p>Broken links, pages returning errors and content that fails an accessibility check are
                raised with the page owner and corrected. Feedback received through the contact form is
                treated as part of this monitoring.</p>
                """, 12,
                "How the availability, quality and accessibility of this portal are monitored.",
                "Website Monitoring"),

            P("policies/content-contribution-policy", "Content Contribution, Moderation and Approval Policy",
                """
                <p>This policy sets out who may contribute content to this portal, who checks it, and who
                approves its publication.</p>
                <h3>Contribution</h3>
                <p>Content originates with the scheme division of the Ministry of Micro, Small and Medium
                Enterprises and with the implementing agencies. Each contributor is responsible for the
                accuracy of what they submit and for ensuring it does not infringe anyone's copyright.</p>
                <h3>Moderation</h3>
                <p>Submitted content is checked for accuracy against the scheme guidelines, for plain and
                unambiguous language in English and Hindi, and for accessibility - meaningful headings,
                alternative text on images, and documents in formats that assistive technology can read.</p>
                <h3>Approval</h3>
                <p>Nothing is published on this portal without the approval of the officer designated for
                the section it belongs to. The content management system records who created, changed and
                published each item, and that record is retained.</p>
                """, 13,
                "Who contributes content to this portal, who checks it, and who approves publication.",
                "Content Contribution"),

            P("policies/website-security-policy", "Website Security Policy",
                """
                <p>This portal is built and operated to keep the information it holds, and the people who
                use it, safe.</p>
                <p>All traffic is served over HTTPS. Access to the content management system requires
                individual named accounts; there are no shared logins, and what each account may do is
                limited to the role it has been given. Administrative actions are logged.</p>
                <p>Uploaded files are checked by type and size and are served so that a browser cannot
                execute them. The application is protected against the common web attacks, including
                cross-site scripting, cross-site request forgery, SQL injection and clickjacking, and it
                sets a content security policy restricting where scripts, styles and framed content may
                come from.</p>
                <p>The portal is subject to a security audit by a CERT-In empanelled auditor before it goes
                live and after any significant change, and to periodic audits thereafter.</p>
                <p>If you believe you have found a security weakness in this portal, please report it
                through the contact form rather than disclosing it publicly, and it will be investigated.</p>
                """, 14,
                "How this portal protects the information it holds and the people who use it.",
                "Security Policy"),

            P("help", "Help",
                """
                <p>This page explains how to use the features of this portal and how to open the documents
                it publishes.</p>
                <h3>Accessibility</h3>
                <p>The accessibility options button on every page lets you enlarge the text, increase
                spacing and line height, raise the contrast, invert the colours, highlight links and turn
                on a reading mask. Your choices are remembered in your own browser and do not change the
                site for anyone else. Press <strong>Ctrl+F2</strong> to open the panel from the keyboard.
                A skip link at the top of every page takes you straight to the main content.</p>
                <p>Guidance for users of screen reading software is on the
                <a href="/screen-reader-access">Screen Reader Access</a> page.</p>
                <h3>Changing the language</h3>
                <p>The language selector in the header uses the Government of India's Bhashini translation
                service to render the portal in other Indian languages.</p>
                <h3>Opening the documents we publish</h3>
                <p>Documents on this portal are published as PDF, and occasionally as Word, Excel or
                PowerPoint files. Most browsers open PDFs without any additional software. If yours does
                not, a PDF reader is available free of charge from
                <a href="https://get.adobe.com/reader/" target="_blank" rel="noopener noreferrer">Adobe</a>,
                and office documents can be opened with any office suite, including the free
                <a href="https://www.libreoffice.org/" target="_blank" rel="noopener noreferrer">LibreOffice</a>.
                Each download on this portal shows its format and size before you open it.</p>
                <h3>If something does not work</h3>
                <p>Please tell us through the contact form, saying which page you were on and what you were
                trying to do, and we will look into it.</p>
                """, 15,
                "How to use the accessibility options, change the language, and open the documents this portal publishes."),
        };

        var addedPages = required.Where(p => !known.Contains(p.Slug)).ToList();
        if (addedPages.Count > 0)
        {
            db.Pages.AddRange(addedPages);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("GIGW top-up: added {Count} pages ({Slugs})",
                addedPages.Count, string.Join(", ", addedPages.Select(p => p.Slug)));
        }

        // ------------------------------------------------------------------ links ----
        // A page nothing links to is a page nobody finds. GIGW asks for these to be
        // reachable from every page, which on this site means the footer.
        var pagesBySlug = await db.Pages
            .Select(p => new { p.Id, p.Slug })
            .ToDictionaryAsync(p => p.Slug, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);

        var menu = await db.MenuItems
            .Select(m => new { m.Location, m.Url, m.PageId })
            .ToListAsync(ct);

        // The footer has two places a link can live - the policies column and the
        // bottom bar - and a link moved from one to the other is still in the footer.
        // Checking the column alone put Screen Reader Access back into it on every
        // start after it had been moved to the bar.
        bool Same(MenuLocation a, MenuLocation b) =>
            a == b || (a is MenuLocation.Footer or MenuLocation.FooterBottom
                       && b is MenuLocation.Footer or MenuLocation.FooterBottom);

        bool HasPageLink(MenuLocation where, string slug) =>
            pagesBySlug.TryGetValue(slug, out var id)
            && menu.Any(m => Same(m.Location, where) && m.PageId == id);

        // Matches on the host, not on a substring of the address. "india.gov.in" is
        // a substring of digitalindia.gov.in and npcindia.gov.in, and a plain Contains
        // would read either of those as the National Portal already being linked.
        bool HasHostLink(MenuLocation where, string host)
            => menu.Any(m => m.Location == where
                             && Uri.TryCreate(m.Url, UriKind.Absolute, out var uri)
                             && (uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase)
                                 || uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase)));

        var nextFooterOrder = menu.Count(m => m.Location == MenuLocation.Footer) + 1;
        var newLinks = new List<MenuItem>();

        void LinkPage(MenuLocation where, string slug, string label)
        {
            if (!pagesBySlug.TryGetValue(slug, out var id) || HasPageLink(where, slug)) return;
            newLinks.Add(new MenuItem
            {
                Location = where,
                Label = label,
                PageId = id,
                SortOrder = nextFooterOrder++,
                IsActive = true
            });
        }

        LinkPage(MenuLocation.Footer, "policies/content-archival-policy", "Content Archival Policy");
        LinkPage(MenuLocation.Footer, "policies/content-review-policy", "Content Review Policy");
        LinkPage(MenuLocation.Footer, "policies/website-monitoring-policy", "Website Monitoring Policy");
        LinkPage(MenuLocation.Footer, "policies/content-contribution-policy", "Content Contribution Policy");
        LinkPage(MenuLocation.Footer, "policies/website-security-policy", "Website Security Policy");

        // Present on the site since launch, but reachable only by typing the address.
        LinkPage(MenuLocation.Footer, "screen-reader-access", "Screen Reader Access");
        LinkPage(MenuLocation.Footer, "help", "Help");

        // GIGW asks every government site to link the National Portal of India.
        if (!HasHostLink(MenuLocation.UsefulLinks, "india.gov.in"))
        {
            newLinks.Add(new MenuItem
            {
                Location = MenuLocation.UsefulLinks,
                Label = "National Portal of India",
                Url = "https://www.india.gov.in/",
                OpenInNewTab = true,
                SortOrder = menu.Count(m => m.Location == MenuLocation.UsefulLinks) + 1,
                IsActive = true
            });
        }

        if (newLinks.Count > 0)
        {
            db.MenuItems.AddRange(newLinks);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("GIGW top-up: added {Count} navigation links", newLinks.Count);
        }
    }
}
