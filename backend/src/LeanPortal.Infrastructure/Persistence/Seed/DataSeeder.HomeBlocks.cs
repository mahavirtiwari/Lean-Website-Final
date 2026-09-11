using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Composes the home page from ordered blocks. The band order follows the reference layout:
    /// hero, quick actions, welcome + video, counters, components grid, documents, ministry
    /// message, scheme levels, login portals, initiatives, testimonials, useful links, CTA, contact.
    /// Editors can hide, retitle and re-order any of these from the admin console.
    /// </summary>
    private static async Task SeedHomeBlocksAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var home = await db.Pages.FirstOrDefaultAsync(p => p.Slug == "home", ct);
        if (home is null || await db.PageBlocks.AnyAsync(b => b.PageId == home.Id, ct)) return;

        // SortOrder is assigned from the final list position below, not here, so that blocks
        // declared as locals before the list still land in their intended band order.
        PageBlock B(BlockType type, string? eyebrow = null, string? heading = null, string? sub = null,
            string? body = null, string? settings = null, string? linkText = null, string? linkUrl = null,
            string? link2Text = null, string? link2Url = null) => new()
        {
            PageId = home.Id,
            Type = type,
            IsVisible = true,
            Eyebrow = eyebrow,
            Heading = heading,
            SubHeading = sub,
            Body = body,
            PrimaryLinkText = linkText,
            PrimaryLinkUrl = linkUrl,
            SecondaryLinkText = link2Text,
            SecondaryLinkUrl = link2Url,
            SettingsJson = settings
        };

        var welcome = B(BlockType.WelcomeVideo,
            eyebrow: "Ministry of Micro, Small and Medium Enterprises",
            heading: "Welcome to the MSME Competitive (LEAN) Scheme",
            sub: "A national programme to make Indian manufacturing MSMEs globally competitive.",
            body:
            "<p>Lean manufacturing considers the expenditure of resources for any goal, other than the creation of " +
            "value for the end customer, to be wasteful and therefore a target for elimination through " +
            "<em>kaizen</em> - continuous improvement.</p>" +
            "<p>Through this scheme the Ministry of MSME helps manufacturing enterprises reduce rejection, " +
            "inventory, movement and cost, while improving quality, safety, capability and profitability. Bronze " +
            "level is free of cost, and consultant fees at the higher levels are subsidised by 90%.</p>");
        welcome.VideoUrl = "https://www.youtube.com/watch?v=w0aExafa1ag";
        welcome.ImageUrl = "/assets/images/home/welcome-lean.svg";

        // The techniques themselves, beside the copy that describes them. Editable
        // from the page builder: empty the list and the block shows the video again.
        welcome.SettingsJson =
            """
            {"toolkitTitle":"The LEAN toolkit",
             "toolkitNote":"Applied on the shop floor with a consultant, level by level.",
             "tools":[
               {"name":"5S","caption":"Sort, set in order, shine, standardise, sustain","icon":"layers"},
               {"name":"Kaizen","caption":"Small improvements, made continuously","icon":"refresh"},
               {"name":"Value stream mapping","caption":"See where the waste actually sits","icon":"map"},
               {"name":"Poka-yoke","caption":"Design the mistake out of the step","icon":"shield"},
               {"name":"Quick changeover","caption":"Shorter setups, smaller batches","icon":"clock"},
               {"name":"TPM","caption":"Machines maintained by the people who run them","icon":"settings"}
             ]}
            """;
        welcome.PrimaryLinkText = "Read more about the scheme";
        welcome.PrimaryLinkUrl = "/about-scheme/introduction";
        welcome.SecondaryLinkText = "Watch the launch video";
        welcome.SecondaryLinkUrl = "https://www.youtube.com/watch?v=w0aExafa1ag";

        var quickActions = B(BlockType.QuickActionCards,
            heading: "Start here",
            sub: "The four things most visitors come to this portal to do.",
            settings:
            """
            {"cards":[
              {"title":"Scheme Guideline","description":"Explore the approved LEAN guidelines to ensure proper implementation.","icon":"file-text","url":"/downloads","linkText":"Download"},
              {"title":"Scheme Brochure","description":"Details of the scheme, Lean concepts, levels and benefits.","icon":"book-open","url":"/downloads","linkText":"Download"},
              {"title":"Launch Video","description":"Watch the launch of the MSME Competitive (LEAN) Scheme.","icon":"play-circle","url":"https://www.youtube.com/watch?v=w0aExafa1ag","linkText":"Watch","external":true},
              {"title":"LEAN LMS","description":"Learning Management System for the LEAN Bronze level.","icon":"graduation-cap","url":"https://msme-leanlms.in/login.aspx","linkText":"Login","external":true}
            ]}
            """);

        var minister = B(BlockType.MinisterMessage,
            eyebrow: "From the Ministry",
            heading: "A roadmap to global competitiveness",
            body:
            "<p>MSMEs form an integral part of almost every value chain, and there is a symbiotic relationship " +
            "between large corporations and relatively small sized suppliers. As global supply chains reconfigure, " +
            "the enterprises that can demonstrate consistent quality, delivery and cost will capture the " +
            "opportunity.</p>" +
            "<p>The MSME Competitive (LEAN) Scheme gives every manufacturing MSME in India a structured, " +
            "subsidised path to that capability. It is voluntary, it is open to every state and union territory, " +
            "and its Bronze level costs nothing.</p>",
            settings:
            """
            {"priorities":[
              {"title":"Productivity & efficiency","icon":"trending-up"},
              {"title":"Quality & competitiveness","icon":"award"},
              {"title":"Resource optimisation","icon":"leaf"},
              {"title":"Digital empowerment","icon":"cpu"}
            ]}
            """);
        minister.ImageUrl = "/assets/images/home/ministry-message.svg";
        minister.PrimaryLinkText = "About the scheme";
        minister.PrimaryLinkUrl = "/about-scheme";

        var cta = B(BlockType.CallToAction,
            heading: "Ready to make your enterprise LEAN?",
            sub: "Registration is free and takes eight steps. All you need is your Udyam Registration Number.");
        cta.PrimaryLinkText = "Apply for the LEAN Scheme";
        cta.PrimaryLinkUrl = "/VerifyUdyam/Register";
        cta.SecondaryLinkText = "See how registration works";
        cta.SecondaryLinkUrl = "/register/how-to-register";
        cta.ImageUrl = "/assets/images/home/cta-background.svg";

        var contact = B(BlockType.ContactStrip,
            heading: "Have a question? Get in touch.",
            sub: "Write to the LEAN Scheme team at the Ministry of MSME, or reach the implementing agency for your region.");
        contact.PrimaryLinkText = "Contact us";
        contact.PrimaryLinkUrl = "/contact-us";

        // The order of this list is the order of the bands down the home page.
        List<PageBlock> blocks =
        [
            B(BlockType.HeroSlider),

            quickActions,

            welcome,

            B(BlockType.StatisticsCounter,
                eyebrow: "Portal analytics",
                heading: "The scheme in numbers",
                sub: "Live participation figures from the LEAN Scheme portal."),

            B(BlockType.SchemeComponentsGrid,
                eyebrow: "What the scheme delivers",
                heading: "Six components of the LEAN Scheme",
                sub: "From nation-wide awareness to a single-window digital platform."),

            B(BlockType.DocumentsNotices,
                eyebrow: "Stay informed",
                heading: "Documents & notices",
                sub: "Guidelines, brochures, circulars and the latest announcements.",
                linkText: "All downloads", linkUrl: "/downloads",
                link2Text: "All news & announcements", link2Url: "/media/news",
                settings:
                """
                {"documentCount":4,"postCount":4,
                 "noticesHeading":"Latest notices",
                 "noticesSubHeading":"Announcements, circulars and news from the Ministry."}
                """),

            minister,

            B(BlockType.SchemeLevels,
                eyebrow: "Your LEAN journey",
                heading: "Three levels, one journey to world class",
                sub: "Take the LEAN Pledge, then progress through Bronze, Silver and Gold.",
                linkText: "Full details of every level", linkUrl: "/about-scheme/scheme-levels"),

            B(BlockType.LoginPortals,
                eyebrow: "Stakeholder access",
                heading: "Registration & login",
                sub: "Sign in to the portal for your role, or register your enterprise."),

            B(BlockType.Initiatives,
                eyebrow: "Capacity building",
                heading: "LEAN initiatives",
                sub: "Awareness programmes for MSMEs, and training for assessors and consultants.",
                settings:
                """
                {"items":[
                  {"title":"Awareness Programmes","audience":"For MSMEs","description":"An in-depth presentation on the scheme, its levels, benefits and how to register.","icon":"megaphone","url":"/programmes/awareness"},
                  {"title":"Training for Assessors","audience":"For professionals","description":"A structured training programme for professionals assessing MSMEs under the scheme.","icon":"clipboard-check","url":"/programmes/training"},
                  {"title":"Training for Consultants","audience":"For professionals","description":"Training for consultants from organisations empanelled by QCI and NPC.","icon":"users","url":"/programmes/training"}
                ]}
                """),

            B(BlockType.Testimonials,
                eyebrow: "Success stories",
                heading: "What LEAN changed on the shop floor",
                sub: "Enterprises that completed Lean implementation, in their own words."),

            B(BlockType.GalleryShowcase,
                eyebrow: "Gallery",
                heading: "The scheme in pictures and film",
                sub: "Photographs and recordings from awareness programmes, training and shop-floor visits.",
                linkText: "Open the full gallery",
                linkUrl: "/gallery"),

            B(BlockType.PartnersStrip,
                eyebrow: "Working with",
                heading: "Our partners",
                sub: "The bodies delivering the scheme alongside the Ministry of MSME."),

            B(BlockType.UsefulLinks,
                heading: "Useful links",
                sub: "Other MSME schemes and services you may need."),

            cta,

            contact
        ];

        for (var i = 0; i < blocks.Count; i++)
            blocks[i].SortOrder = i + 1;

        db.PageBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} home page blocks.", blocks.Count);
    }
}
