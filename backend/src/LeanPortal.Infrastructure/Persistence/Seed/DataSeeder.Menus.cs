using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Builds the main navigation (mirroring the existing LEAN portal), the footer columns
    /// the footer columns and the useful-links strip.
    /// </summary>
    private static async Task SeedMenusAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.MenuItems.AnyAsync(ct)) return;

        var pages = await db.Pages.ToDictionaryAsync(p => p.Slug, p => p.Id, ct);
        int? PageId(string slug) => pages.TryGetValue(slug, out var id) ? id : null;

        MenuItem M(MenuLocation location, string label, int order, string? url = null, string? slug = null,
            string? icon = null, bool newTab = false, bool highlighted = false) => new()
        {
            Location = location,
            Label = label,
            SortOrder = order,
            Url = url,
            PageId = slug is null ? null : PageId(slug),
            Icon = icon,
            OpenInNewTab = newTab,
            IsHighlighted = highlighted
        };

        // ------------------------------------------------------------ main menu ----
        var home = M(MenuLocation.Main, "Home", 1, "/", "home");
        var about = M(MenuLocation.Main, "About Scheme", 2, "/about-scheme", "about-scheme");
        var agency = M(MenuLocation.Main, "Implementation Agency", 3, "/implementation-agency", "implementation-agency");
        var consultant = M(MenuLocation.Main, "Register as Consultant", 4, "/register-as-consultant", "register-as-consultant");
        var gallery = M(MenuLocation.Main, "Gallery", 5, "/gallery", "gallery");
        var contact = M(MenuLocation.Main, "Contact Us", 6, "/contact-us", "contact-us");
        var register = M(MenuLocation.Main, "Register for LEAN Scheme", 7, "/register", "register", highlighted: true);

        db.MenuItems.AddRange(home, about, agency, consultant, gallery, contact, register);
        await db.SaveChangesAsync(ct);

        var aboutChildren = new[]
        {
            M(MenuLocation.Main, "Introduction", 1, "/about-scheme/introduction", "about-scheme/introduction"),
            M(MenuLocation.Main, "Objective", 2, "/about-scheme/objective", "about-scheme/objective"),
            M(MenuLocation.Main, "Scheme Components", 3, "/about-scheme/scheme-components", "about-scheme/scheme-components"),
            M(MenuLocation.Main, "Scheme Levels", 4, "/about-scheme/scheme-levels", "about-scheme/scheme-levels"),
            M(MenuLocation.Main, "Coverage & Eligibility", 5, "/about-scheme/coverage-eligibility", "about-scheme/coverage-eligibility"),
            M(MenuLocation.Main, "E-Certificate", 6, "/about-scheme/e-certificate", "about-scheme/e-certificate"),
            M(MenuLocation.Main, "Financial Assistance", 7, "/about-scheme/financial-assistance", "about-scheme/financial-assistance")
        };
        foreach (var child in aboutChildren) child.ParentId = about.Id;

        var agencyChildren = new[]
        {
            M(MenuLocation.Main, "Quality Council of India (QCI)", 1, "https://qcin.org/", newTab: true),
            M(MenuLocation.Main, "National Productivity Council (NPC)", 2, "https://www.npcindia.gov.in/NPC/User/", newTab: true),
            M(MenuLocation.Main, "Awareness Programmes", 3, "/programmes/awareness", "programmes/awareness"),
            M(MenuLocation.Main, "Training Programmes", 4, "/programmes/training", "programmes/training")
        };
        foreach (var child in agencyChildren) child.ParentId = agency.Id;

        var contactChildren = new[]
        {
            M(MenuLocation.Main, "Ministry of MSME", 1, "/contact-us", "contact-us"),
            M(MenuLocation.Main, "QCI", 2, "https://ndie.qcin.org/contact-us/", newTab: true),
            M(MenuLocation.Main, "NPC", 3, "https://www.npcindia.gov.in/NPC/User/ContactUs", newTab: true)
        };
        foreach (var child in contactChildren) child.ParentId = contact.Id;

        var registerChildren = new[]
        {
            M(MenuLocation.Main, "Apply for LEAN Scheme", 1, "/VerifyUdyam/Register", newTab: true),
            M(MenuLocation.Main, "OEM Register", 2, "/OEM/RegisterNew", newTab: true),
            M(MenuLocation.Main, "How to Register", 3, "/register/how-to-register", "register/how-to-register"),
            M(MenuLocation.Main, "Benefits to MSMEs", 4, "/register/benefits-to-msme", "register/benefits-to-msme")
        };
        foreach (var child in registerChildren) child.ParentId = register.Id;

        db.MenuItems.AddRange(aboutChildren);
        db.MenuItems.AddRange(agencyChildren);
        db.MenuItems.AddRange(contactChildren);
        db.MenuItems.AddRange(registerChildren);

        // The masthead carries no utility strip. Screen Reader Access and Sitemap
        // are in the footer policies column, and the ministry link is in Useful
        // Links, so nothing that used to sit up there became unreachable.

        // ---------------------------------------------------------- quick links ----
        db.MenuItems.AddRange(
            M(MenuLocation.QuickLinks, "About the Scheme", 1, "/about-scheme", "about-scheme"),
            M(MenuLocation.QuickLinks, "Scheme Levels", 2, "/about-scheme/scheme-levels", "about-scheme/scheme-levels"),
            M(MenuLocation.QuickLinks, "How to Register", 3, "/register/how-to-register", "register/how-to-register"),
            M(MenuLocation.QuickLinks, "Financial Assistance", 4, "/about-scheme/financial-assistance", "about-scheme/financial-assistance"),
            M(MenuLocation.QuickLinks, "Downloads", 5, "/downloads", "downloads"),
            M(MenuLocation.QuickLinks, "Gallery", 6, "/gallery", "gallery"),
            M(MenuLocation.QuickLinks, "News & Announcements", 7, "/media/news", "media/news"),
            M(MenuLocation.QuickLinks, "FAQs", 8, "/faqs", "faqs")
        );

        // --------------------------------------------------------- useful links ----
        db.MenuItems.AddRange(
            M(MenuLocation.UsefulLinks, "Udyam Registration", 1, "https://udyamregistration.gov.in/", newTab: true),
            M(MenuLocation.UsefulLinks, "ZED Certification", 2, "https://zed.msme.gov.in/", newTab: true),
            M(MenuLocation.UsefulLinks, "MSME Innovative Scheme", 3, "https://innovative.msme.gov.in/", newTab: true),
            M(MenuLocation.UsefulLinks, "LEAN LMS", 4, "https://msme-leanlms.in/login.aspx", newTab: true),
            M(MenuLocation.UsefulLinks, "MyGov", 5, "https://www.mygov.in/", newTab: true),
            M(MenuLocation.UsefulLinks, "India.gov.in", 6, "https://www.india.gov.in/", newTab: true),
            M(MenuLocation.UsefulLinks, "Digital India", 7, "https://www.digitalindia.gov.in/", newTab: true),
            M(MenuLocation.UsefulLinks, "Ministry of MSME", 8, "https://msme.gov.in/", newTab: true)
        );

        // ---------------------------------------------------------------- footer ----
        db.MenuItems.AddRange(
            M(MenuLocation.Footer, "Copyright Policy", 1, "/policies/copyright-policy", "policies/copyright-policy"),
            M(MenuLocation.Footer, "Hyperlinking Policy", 2, "/policies/hyperlinking-policy", "policies/hyperlinking-policy"),
            M(MenuLocation.Footer, "Privacy Policy", 3, "/policies/privacy-policy", "policies/privacy-policy"),
            M(MenuLocation.Footer, "Terms & Conditions", 4, "/policies/terms-and-conditions", "policies/terms-and-conditions"),
            M(MenuLocation.Footer, "Accessibility Statement", 5, "/policies/accessibility-statement", "policies/accessibility-statement"),
            M(MenuLocation.Footer, "Disclaimer", 6, "/policies/disclaimer", "policies/disclaimer"),
            M(MenuLocation.Footer, "FAQs", 7, "/faqs", "faqs"),
            M(MenuLocation.Footer, "Sitemap", 8, "/sitemap", "sitemap"),
            // Also in the top strip, but that strip narrows on small screens, and
            // GIGW expects this link to be reachable from every page on every device.
            M(MenuLocation.Footer, "Screen Reader Access", 9, "/screen-reader-access", "screen-reader-access")
        );

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded navigation menus.");
    }
}
