using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// The Benefits / Incentives band and the pages behind it.
    ///
    /// Four cards, one per source of support, each opening a page that sets out what
    /// that body offers. The categories are rows rather than a list in a template
    /// because which bodies offer incentives changes: a state withdraws a subsidy, a
    /// new central scheme starts, and neither should need a release.
    ///
    /// All three parts are top-ups. The rows, the pages and the home-page band are
    /// each added only when absent, so an installation seeded before today receives
    /// the section without losing anything an editor has since changed.
    /// </summary>
    private static async Task SeedBenefitsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // ------------------------------------------------------------------ pages ----
        var known = (await db.Pages.Select(p => p.Slug).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Page P(string slug, string title, string body, int order, string summary,
               string? shortTitle = null, int? parentId = null) => new()
        {
            Slug = slug,
            Title = title,
            ShortTitle = shortTitle,
            Summary = summary,
            Body = body,
            Template = PageTemplate.SidebarLeft,
            BannerCaption = "Benefits and incentives",
            SortOrder = order,
            ParentId = parentId,
            Status = PublishStatus.Published,
            PublishedAt = now,
            MetaTitle = title,
            MetaDescription = summary,
            ShowSidebarNav = true
        };

        var parent = await db.Pages.FirstOrDefaultAsync(p => p.Slug == "benefits-incentives", ct);

        if (parent is null && !known.Contains("benefits-incentives"))
        {
            parent = P("benefits-incentives", "Benefits / Incentives",
                """
                <p>Support for enterprises taking part in the MSME Competitive (LEAN) Scheme comes
                from more than one place. The Ministry of Micro, Small and Medium Enterprises meets
                most of the cost of implementation directly; states and union territories add their
                own incentives; banks and financial institutions recognise certification in their
                lending; and other central ministries and organisations extend benefits of their
                own to certified units.</p>
                <p>Use the sections below to see what each offers and who to approach. Where an
                incentive is administered by another body, the conditions are theirs and are
                applied by them - this portal records what has been notified, not an entitlement.</p>
                """, 6,
                "Financial and non-financial support available to enterprises under the LEAN Scheme, "
                + "from the ministry, the states, the banks and other organisations.",
                shortTitle: "Benefits / Incentives");

            db.Pages.Add(parent);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Benefits: added the parent page");
        }

        if (parent is not null)
        {
            var children = new List<Page>
            {
                P("benefits-incentives/ministry-of-msme", "Incentives by the Ministry of MSME",
                    """
                    <p>The Ministry meets the greater part of the cost of implementing Lean, so that
                    the price of getting started is not what keeps a small enterprise out.</p>
                    <h3>Cost of implementation</h3>
                    <p>The Ministry contributes the major share of the LEAN consultant's fee at each
                    level of implementation. Micro, small and medium enterprises pay only the balance,
                    and the contribution is released against verified completion rather than in
                    advance.</p>
                    <h3>Additional contribution</h3>
                    <p>A higher share is contributed for enterprises owned by women, and by
                    entrepreneurs from the Scheduled Castes and Scheduled Tribes, and for units in
                    the North Eastern Region and other areas the Ministry notifies from time to
                    time.</p>
                    <h3>Handholding and certification</h3>
                    <p>Awareness programmes, workshops and the LEAN learning platform are provided at
                    no cost to the enterprise, and the certificate issued on successful completion of
                    a level carries no separate fee.</p>
                    <p>The current rates and conditions are those in the scheme guidelines published
                    under <a href="/downloads">Downloads</a>, which prevail over any summary here.</p>
                    """, 1,
                    "What the Ministry of MSME contributes towards implementation, and the additional "
                    + "share for women, SC/ST entrepreneurs and units in the North Eastern Region.",
                    shortTitle: "Ministry of MSME", parentId: parent.Id),

                P("benefits-incentives/states-uts", "Incentives by States and Union Territories",
                    """
                    <p>Several states and union territories offer their own support to enterprises
                    that take up Lean, in addition to what the Ministry contributes. These are
                    notified and administered by the state concerned.</p>
                    <p>What is offered varies, and commonly includes reimbursement of part of the
                    enterprise's own share of the implementation cost, capital and interest subsidy
                    linked to certification, preference or relaxation in state procurement, and
                    concessions in state industrial policy for certified units.</p>
                    <p>Because these are state schemes, the eligibility conditions, the rates and the
                    application route are set by the state's industries or MSME department. Contact
                    the department in the state where the unit is located, or the district industries
                    centre, for what currently applies there.</p>
                    <p>If your state has notified an incentive that is not reflected here, please
                    tell us through the <a href="/contact-us">contact form</a> so the page can be
                    brought up to date.</p>
                    """, 2,
                    "Support notified by individual states and union territories for enterprises "
                    + "implementing Lean, and who to approach for it.",
                    shortTitle: "States / UTs", parentId: parent.Id),

                P("benefits-incentives/financial-institutions", "Incentives by Banks and Financial Institutions",
                    """
                    <p>Lean certification is evidence that an enterprise has reduced waste, shortened
                    its cycle times and put measurement behind its processes. Banks and financial
                    institutions take that into account when they assess a unit.</p>
                    <p>Depending on the institution, the benefits reported by certified enterprises
                    include better terms on working capital and term lending, faster appraisal where
                    certification is accepted as part of the assessment, and easier access to the
                    credit guarantee and refinancing facilities available to MSMEs.</p>
                    <p>These are decisions of the lender, taken under its own credit policy. Nothing
                    on this page is an assurance of credit or of any particular rate: speak to your
                    bank's MSME desk with your certificate and your implementation report.</p>
                    """, 3,
                    "How Lean certification is recognised by banks and financial institutions in "
                    + "lending to MSMEs.",
                    shortTitle: "Financial Institutions", parentId: parent.Id),

                P("benefits-incentives/other-incentives", "Incentives by Other Ministries and Organisations",
                    """
                    <p>Certified enterprises are also recognised by other central ministries, public
                    sector undertakings and industry organisations, each under its own scheme.</p>
                    <p>These commonly take the form of recognition in vendor development and
                    supplier-registration programmes run by public sector undertakings and original
                    equipment manufacturers, preference or relaxation in tender conditions where the
                    procuring body has notified it, support for participation in exhibitions, buyer
                    meets and trade delegations, and eligibility for awards and recognition
                    schemes.</p>
                    <p>Each is administered by the organisation offering it, and its conditions are
                    theirs. Where a benefit requires proof of certification, the certificate issued
                    under this scheme can be verified from
                    <a href="/about-scheme/e-certificate">e-Certificate</a>.</p>
                    """, 4,
                    "Recognition and benefits extended to Lean certified enterprises by other "
                    + "central ministries, public sector undertakings and industry organisations.",
                    shortTitle: "Other Incentives", parentId: parent.Id),
            };

            var missing = children.Where(c => !known.Contains(c.Slug)).ToList();
            if (missing.Count > 0)
            {
                db.Pages.AddRange(missing);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Benefits: added {Count} pages", missing.Count);
            }
        }

        // ------------------------------------------------------------------- cards ----
        if (!await db.Benefits.AnyAsync(ct))
        {
            db.Benefits.AddRange(
                new Benefit
                {
                    Title = "Ministry of MSME",
                    Subtitle = "Incentives by MoMSME through its schemes",
                    Icon = "building",
                    LinkUrl = "/benefits-incentives/ministry-of-msme",
                    LinkText = "View",
                    OpenInNewTab = true,
                    SortOrder = 1
                },
                new Benefit
                {
                    Title = "States / UTs",
                    Subtitle = "Incentives by States and Union Territories",
                    Icon = "map",
                    LinkUrl = "/benefits-incentives/states-uts",
                    LinkText = "View",
                    OpenInNewTab = true,
                    SortOrder = 2
                },
                new Benefit
                {
                    Title = "Financial Institutions",
                    Subtitle = "Incentives by banks",
                    Icon = "handshake",
                    LinkUrl = "/benefits-incentives/financial-institutions",
                    LinkText = "View",
                    OpenInNewTab = true,
                    SortOrder = 3
                },
                new Benefit
                {
                    Title = "Other Incentives",
                    Subtitle = "Incentives by central ministries and other organisations",
                    Icon = "award",
                    LinkUrl = "/benefits-incentives/other-incentives",
                    LinkText = "View",
                    OpenInNewTab = true,
                    SortOrder = 4
                });

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Benefits: added 4 cards");
        }

        // ------------------------------------------------- pages become listings ----
        // These four were first published as prose. They are listings now, following
        // the ZED portal: the wording already written stays as the introduction above
        // the list, and the incentives themselves come from the Incentives screen.
        var listingSlugs = new[]
        {
            "benefits-incentives/ministry-of-msme",
            "benefits-incentives/states-uts",
            "benefits-incentives/financial-institutions",
            "benefits-incentives/other-incentives",
        };

        var toConvert = await db.Pages
            .Where(p => listingSlugs.Contains(p.Slug) && p.CustomComponent == null)
            .ToListAsync(ct);

        if (toConvert.Count > 0)
        {
            foreach (var page in toConvert)
            {
                page.Template = PageTemplate.Custom;
                page.CustomComponent = "incentives";
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Benefits: converted {Count} pages to incentive listings",
                toConvert.Count);
        }

        // -------------------------------------------------------------- incentives ----
        // Only what the scheme itself publishes. The other three categories are
        // seeded empty on purpose: a state's incentive, a bank's concession and the
        // officer to contact about it are matters of fact this codebase has no way to
        // know, and inventing them would put false official information in front of an
        // enterprise deciding whether to apply. The screen is there for the ministry
        // to enter them.
        if (!await db.Incentives.AnyAsync(ct))
        {
            const string ministry = "Ministry of Micro, Small and Medium Enterprises";

            Incentive I(string title, string description, string? level = null, int order = 0) => new()
            {
                Category = IncentiveCategory.Ministry,
                Level = level,
                Title = title,
                Description = description,
                IssuerName = ministry,
                SortOrder = order,
                AvailUrl = "/register",
                AvailLabel = "Apply"
            };

            db.Incentives.AddRange(
                I("Subsidy on the cost of LEAN implementation",
                  "The Ministry contributes the major share of the LEAN consultant's fee, up to 90% "
                  + "of the cost of implementation. The enterprise pays only the balance, and the "
                  + "contribution is released against verified completion rather than in advance.",
                  order: 1),

                I("Additional contribution for women, SC and ST owned enterprises",
                  "A higher share of the implementation cost is contributed for enterprises owned by "
                  + "women and by entrepreneurs from the Scheduled Castes and Scheduled Tribes.",
                  order: 2),

                I("Additional contribution for the North Eastern Region and notified areas",
                  "A higher share is also contributed for units in the North Eastern Region and in "
                  + "the other areas the Ministry notifies from time to time.",
                  order: 3),

                I("Free awareness programmes and workshops",
                  "Awareness programmes, workshops and the LEAN learning platform are provided at no "
                  + "cost to the enterprise.",
                  order: 4),

                I("Certification at no separate fee",
                  "The certificate issued on successful completion of a level carries no separate "
                  + "fee. It can be verified from the e-Certificate page.",
                  order: 5));

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Benefits: added 5 ministry incentives");
        }

        // The View buttons open their listing in a new tab, as the ZED portal does.
        // Applied to the rows already saved as well as to the seed above, since these
        // four were published before that was asked for.
        var tabless = await db.Benefits.Where(b => !b.OpenInNewTab).ToListAsync(ct);
        if (tabless.Count > 0)
        {
            foreach (var card in tabless) card.OpenInNewTab = true;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Benefits: {Count} cards now open in a new tab", tabless.Count);
        }

        // --------------------------------------------------------------- home band ----
        var home = await db.Pages.FirstOrDefaultAsync(p => p.Slug == "home", ct);
        if (home is null) return;

        var blocks = await db.PageBlocks.Where(b => b.PageId == home.Id)
            .OrderBy(b => b.SortOrder).ToListAsync(ct);

        if (blocks.Count == 0 || blocks.Any(b => b.Type == BlockType.BenefitsIncentives)) return;

        // Directly below the analytics band, which is where it was asked for. Every
        // block after it moves down one rather than the new band being appended to the
        // foot of the page.
        var analytics = blocks.FirstOrDefault(b => b.Type == BlockType.StatisticsCounter);
        var at = analytics is null ? blocks.Count : blocks.IndexOf(analytics) + 1;

        var band = new PageBlock
        {
            PageId = home.Id,
            Type = BlockType.BenefitsIncentives,
            IsVisible = true,
            Eyebrow = "Support available",
            Heading = "Benefits / Incentives",
            SubHeading = "What the ministry, the states, the banks and other organisations offer "
                         + "enterprises taking up Lean.",
            PrimaryLinkText = "All benefits",
            PrimaryLinkUrl = "/benefits-incentives"
        };

        blocks.Insert(at, band);
        for (var i = 0; i < blocks.Count; i++) blocks[i].SortOrder = i;

        db.PageBlocks.Add(band);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Benefits: inserted the home band below the analytics band");
    }
}
