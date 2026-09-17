using LeanPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Cards within a settings tab. The first prefix that matches a key wins, so the specific
    /// entries are listed before the general ones they sit under.
    /// </summary>
    private static readonly (string Prefix, string Section)[] SettingSections =
    [
        ("site.logo", "Logos and marks"),
        ("site.ministryLogo", "Logos and marks"),
        ("site.footerLogo", "Logos and marks"),
        ("site.favicon", "Logos and marks"),

        ("theme.", "Colour theme"),
        ("site.", "Identity"),

        ("contact.enquiryCategories", "Enquiry form"),
        ("contact.defaultAgency", "Agency choice on the form"),
        ("contact.address", "Address"),
        ("contact.city", "Address"),
        ("contact.pincode", "Address"),
        ("contact.country", "Address"),
        ("contact.mapEmbedUrl", "Address"),
        ("contact.", "How people reach us"),

        ("mail.", "Mail server"),
        ("social.", "Social profiles"),
        // Before the general links. rule: this one belongs with the transactional
        // addresses it is the base for, not with the outward portal links.
        ("links.leanApp", "Transactional LEAN system"),
        ("links.", "Government portals"),
        ("app.", "Transactional LEAN system"),

        // Each panel switch sits in the card it governs, so only the rail-wide one
        // is left in a card of its own.
        ("feature.sidebarDocuments", "Key documents panel"),
        ("feature.sidebarHelp", "Help panel"),
        ("feature.sidebarApply", "Apply panel"),
        ("feature.sidebarWidgets", "The whole rail"),
        ("feature.contactAgencyChoice", "Agency choice on the form"),
        ("feature.", "Site-wide switches"),

        ("sidebar.documents", "Key documents panel"),
        ("sidebar.help", "Help panel"),
        ("sidebar.apply", "Apply panel"),
        ("faqs.", "FAQ page panel"),

        ("footer.", "Footer wording"),
        ("stats.", "Visitor counter"),

        ("seo.", "Search engines"),
        ("analytics.", "Analytics")
    ];

    private static string? SectionFor(string key) =>
        SettingSections.FirstOrDefault(s => key.StartsWith(s.Prefix, StringComparison.Ordinal)).Section;

    /// <summary>
    /// Tops the settings table up rather than filling it once. Anything missing is added with
    /// its default, and the labelling of what is already there is brought back into line - but
    /// never the value, which is whatever an editor last saved.
    /// </summary>
    private static async Task SeedSettingsAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        SiteSetting S(string key, string? value, string display, string group, string type = "text",
            int order = 0, bool pub = true, string? desc = null) => new()
        {
            Key = key,
            Value = value,
            DisplayName = display,
            Group = group,
            Section = SectionFor(key),
            DataType = type,
            SortOrder = order,
            IsPublic = pub,
            Description = desc
        };

        SiteSetting[] defaults =
        [
            // --- Identity -------------------------------------------------------------
            S("site.name", "MSME Competitive (LEAN) Scheme", "Site name", "General", order: 1),
            S("site.tagline", "Enhancing the competitiveness of Indian MSMEs through Lean manufacturing",
                "Tagline", "General", order: 3,
                desc: "One line under the lockups in the footer."),
            S("site.ministry", "Ministry of Micro, Small and Medium Enterprises", "Ministry", "General", order: 4),
            // The strip at the very top carries the ministry in both languages. The
            // Hindi was written into the page and nothing else, so an editor who
            // changed the English name left the two disagreeing.
            S("site.ministryHindi", "सूक्ष्म, लघु और " +
                "मध्यम उद्यम मंत्रालय",
                "Ministry (Hindi)", "General", order: 5,
                desc: "Shown beside the English name in the government strip at the top of every page."),
            S("site.government", "Government of India", "Government", "General", order: 6),
            // The four logo places, each with the address it opens and the words a
            // screen reader says for it. Edited together under Branding in the console.
            S("site.ministryLogoUrl", "/assets/images/brand/msme-logo.svg", "Header logo, left - image", "General", "image", 6,
                desc: "Shown on the left of the masthead. By default the Ministry of MSME lockup, including the State Emblem."),
            S("site.ministryLogoLink", "https://www.msme.gov.in/", "Header logo, left - opens", "General", "url", 7,
                desc: "A full https:// address opens in a new tab; a path on this site such as / opens in place."),
            S("site.ministryLogoAlt", "", "Header logo, left - description", "General", order: 8,
                desc: "What a screen reader says for the logo. Empty means the ministry's name."),
            S("site.logoUrl", "/assets/images/brand/lean-logo.png", "Header logo, right - image", "General", "image", 9,
                desc: "Shown on the right of the masthead. By default the MCLS scheme mark."),
            S("site.logoLink", "/", "Header logo, right - opens", "General", "url", 10,
                desc: "/ is the home page. A full https:// address opens in a new tab."),
            S("site.logoAlt", "", "Header logo, right - description", "General", order: 11,
                desc: "What a screen reader says for the logo. Empty means the scheme's name."),
            S("site.ministryLogoWhiteUrl", "/assets/images/brand/msme-logo-white.svg",
                "Footer logo - image", "General", "image", 12,
                desc: "Shown on the dark footer band, so a light or white version of the logo."),
            S("site.footerLogoLink", "https://www.msme.gov.in/", "Footer logo - opens", "General", "url", 13),
            S("site.footerLogoAlt", "", "Footer logo - description", "General", order: 14,
                desc: "Empty means the ministry's name."),
            S("site.footerLogo2Url", "", "Second footer logo - image", "General", "image", 15,
                desc: "Optional. Shown beside the footer logo, for a partner or the scheme mark. Empty shows nothing."),
            S("site.footerLogo2Link", "", "Second footer logo - opens", "General", "url", 16),
            S("site.footerLogo2Alt", "", "Second footer logo - description", "General", order: 17),
            S("site.faviconUrl", "/favicon.ico", "Favicon", "General", "image", 18),

            // --- Theme ----------------------------------------------------------------
            S("theme.preset", "petrol", "Colour theme", "Appearance", order: 1,
                desc: "petrol, ministry-blue, forest-green, heritage-maroon, royal-purple, saffron-charcoal, " +
                      "indigo-slate, or custom. Chosen with swatches under Branding."),
            S("theme.customPrimary", "#0f7989", "Custom theme - accent colour", "Appearance", order: 2,
                desc: "Used when the theme is custom. Must carry white text at 4.5:1 or better."),
            S("theme.customDark", "#25333f", "Custom theme - dark colour", "Appearance", order: 3,
                desc: "The header strip, footer and dark bands. Must carry white text at 7:1 or better."),

            // --- Contact --------------------------------------------------------------
            S("contact.addressLine1", "Ministry of Micro, Small and Medium Enterprises",
                "Address line 1", "Contact", order: 1),
            S("contact.addressLine2", "Kartavya Bhawan - 03, Kartavya Path", "Address line 2", "Contact", order: 2),
            S("contact.city", "New Delhi", "City", "Contact", order: 3),
            S("contact.pincode", "110001", "PIN code", "Contact", order: 4),
            S("contact.country", "India", "Country", "Contact", order: 5),
            S("contact.helpline", "1800-XXX-XXXX", "Helpline number", "Contact", order: 6,
                desc: "Toll-free scheme helpline shown in the header and footer."),
            S("contact.phone", "011-2306 3800", "Office telephone", "Contact", order: 7),
            S("contact.email", "lean-msme@gov.in", "General enquiry e-mail", "Contact", "email", 8),
            S("contact.supportEmail", "support-lean@gov.in", "Technical support e-mail", "Contact", "email", 9),
            S("contact.workingHours", "Monday to Friday, 9:30 AM - 6:00 PM", "Working hours", "Contact", order: 10),
            S("contact.mapEmbedUrl", "", "Google Maps embed URL", "Contact", "url", 11),

            // Left empty deliberately: with no address the portal's own form collects
            // the enquiry. Paste a Zoho form's embed address here and that form takes
            // the panel over instead, with no deploy. Only https is accepted.
            S("contact.formEmbedUrl", "", "Hosted enquiry form (embed URL)", "Contact", "url", 13,
                desc: "Leave empty to use the portal's own form. A Zoho or similar embed " +
                      "address shown here replaces it, and that service then holds the enquiries."),
            S("contact.tabLabel", "Contact Us", "Side tab label", "Contact", order: 14,
                desc: "Wording on the tab pinned to the right of every page."),

            // Every heading and line of prose on the contact page. Clearing one removes
            // what it labels rather than falling back, which is how a section is taken
            // off the page without a deploy.
            S("contact.formTitle", "Send us a message", "Form heading", "Contact", order: 20),
            S("contact.formIntro",
                "Complete the form and our team will respond.",
                "Form intro", "Contact", "textarea", 21),
            S("contact.agencyLegend", "Who is your enquiry for?", "Agency choice heading",
                "Contact", order: 22),
            S("contact.callTitle", "Call or send us an email", "Contact panel heading",
                "Contact", order: 24),
            S("contact.visitTitle", "Visit our office", "Address panel heading", "Contact", order: 25),
            S("contact.agenciesEyebrow", "Delivering the scheme", "Agencies eyebrow", "Contact", order: 26),
            S("contact.agenciesTitle", "Implementing agencies", "Agencies heading", "Contact", order: 27),
            S("contact.agenciesLead",
                "Handholding, assessment and training are delivered through these bodies. " +
                "Reach them directly for anything specific to your implementation.",
                "Agencies intro", "Contact", "textarea", 28),
            S("contact.attachmentsLabel", "Attachments", "Attachments label", "Contact", order: 29),
            S("contact.verificationLabel", "Verification", "Verification label", "Contact", order: 30),

            S("feature.contactAgencyChoice", "true", "Ask which implementing agency the enquiry is for",
                "Contact", "boolean", 40,
                desc: "Off, or with only one agency active, the question is not asked and every enquiry " +
                      "goes to the agency below."),
            S("contact.defaultAgency", "", "Agency enquiries go to when the question is not asked",
                "Contact", order: 41,
                desc: "The agency's short name, e.g. QCI or NPC. Empty sends them to the general enquiry e-mail."),

            // One category per line; the contact form's subject list is built from this.
            S("contact.enquiryCategories",
                """
                Scheme information
                Registration and Udyam
                Handholding and consultants
                Certification
                Technical / portal issue
                Grievance
                Other
                """,
                "Enquiry categories", "Contact", "textarea", 12,
                desc: "Subjects offered on the contact form, one per line."),

            // --- Social ---------------------------------------------------------------
            S("social.twitter", "https://twitter.com/MCLS_2022", "X / Twitter", "Social", "url", 1),
            S("social.facebook", "https://www.facebook.com/minmsme", "Facebook", "Social", "url", 2),
            S("social.linkedin", "https://www.linkedin.com/company/ministry-of-msme", "LinkedIn", "Social", "url", 3),
            S("social.youtube", "https://www.youtube.com/@ministryofmsme", "YouTube", "Social", "url", 4),
            S("social.twitterHandle", "MCLS_2022", "X / Twitter handle", "Social", order: 5),

            // --- External systems -----------------------------------------------------
            S("links.msmeMinistry", "https://www.msme.gov.in/", "Ministry of MSME", "External links", "url", 5,
                desc: "Where the ministry lockup in the masthead and footer points."),

            // --- Application deep links (transactional LEAN system) --------------------
            // The base every relative transactional address is resolved against - the
            // login tiles' URLs and app.msmeRegister below. It was read by the site but
            // never seeded, so it fell back to a constant in the code: if the
            // transactional system moved, there was no way to follow it without a
            // release.
            S("links.leanApp", "https://lean.msme.gov.in", "Transactional LEAN system base URL",
                "Application", "url", 0,
                desc: "Relative addresses on this tab and on the login tiles are resolved against this."),
            // Which paths belong to the application rather than to this site. A menu
            // item pointing at one of these is sent there; anything else is a page
            // here, and a menu entry for a screen not on this list used to look right
            // in the console and land on "page not found" on the site.
            S("links.leanAppPaths", "/VerifyUdyam, /OEM/, /www/, /AgencyLogin",
                "Paths that belong to the LEAN system", "Application", "text", 1,
                desc: "Separated by commas. A menu or link starting with one of these opens on the " +
                      "transactional system above instead of looking for a page on this site."),
            S("app.msmeRegister", "/VerifyUdyam/Register", "MSME registration URL", "Application", "url", 2),

            // --- Feature toggles ------------------------------------------------------
            S("feature.newsTicker", "true", "Show the What is New ticker", "Features", "boolean", 1),
            S("feature.testimonials", "true", "Show success stories", "Features", "boolean", 3),
            S("feature.bhashini", "true", "Show the Bhashini language selector", "Features", "boolean", 4,
                desc: "Turns off the translation control in the masthead. The portal stays fully usable in English."),
            S("feature.accessibilityTools", "true", "Show the accessibility toolbar", "Features", "boolean", 5,
                desc: "Leave on: the reader toolkit is part of the portal's GIGW and WCAG commitment."),
            S("feature.homeGallery", "true", "Show the gallery band on the home page", "Features", "boolean", 6,
                desc: "The photographs and film rails below the success stories."),
            S("feature.contactTab", "true", "Show the contact tab on every page", "Features", "boolean", 7,
                desc: "The tab pinned to the right edge of every page. Hidden on phones either way."),
            S("feature.helpline", "true", "Show the helpline in the menu", "Features", "boolean", 8),
            S("feature.visitorCount", "true", "Show the visitor counter", "Features", "boolean", 8,
                desc: "The running total in the footer. Turning this off also stops the count being recorded."),
            S("feature.chatbot", "false", "Show the ask-a-question assistant", "Features", "boolean", 9,
                desc: "Answers by quoting published pages, FAQs, documents and notices, with a " +
                      "link to each. It never writes an answer of its own."),
            S("maintenance.title", "This portal is temporarily unavailable",
                "Maintenance notice: heading", "Features", order: 11,
                desc: "Shown to the public while maintenance mode is on."),
            S("maintenance.message",
                "The portal is closed for scheduled maintenance and will be back shortly. " +
                "We are sorry for the inconvenience.",
                "Maintenance notice: message", "Features", "multiline", 12),
            // --- Mail ------------------------------------------------------------------
            // All private: an outgoing mail server, its user and its password are not
            // for the public settings response. The password is additionally encrypted
            // before it is stored - see SecretProtector.
            S("mail.host", "smtp.office365.com", "Mail server", "Mail", "text", 1, pub: false,
                desc: "For Microsoft 365 / Outlook this is smtp.office365.com. Your own relay may differ."),
            S("mail.port", "587", "Port", "Mail", "number", 2, pub: false,
                desc: "587 with STARTTLS is what Microsoft 365 and most relays use."),
            S("mail.encryption", "StartTls", "Encryption", "Mail", "text", 3, pub: false,
                desc: "StartTls for port 587, or None for an internal relay that does not encrypt."),
            S("mail.username", "", "Sign-in name", "Mail", "text", 4, pub: false,
                desc: "The mailbox the portal signs in as. Leave both this and the password " +
                      "empty for a relay that accepts mail without signing in."),
            S("mail.password", "", "Password", "Mail", "password", 5, pub: false,
                desc: "Stored encrypted. Microsoft 365 accounts with multi-factor authentication " +
                      "need an app password rather than the account password."),
            S("mail.fromAddress", "", "Send as", "Mail", "email", 6, pub: false,
                desc: "The address enquiries appear to come from. Microsoft 365 requires the " +
                      "sign-in mailbox to be allowed to send as this address."),
            S("mail.fromName", "MSME Competitive (LEAN) Scheme", "Sender name", "Mail", "text", 7, pub: false),
            S("mail.copyTo", "", "Blind copy to", "Mail", "email", 8, pub: false,
                desc: "Optional. Copies the ministry on every enquiry sent to an agency."),

            S("feature.pageUpdatedDate", "true", "Show the last-updated date on content pages",
                "Features", "boolean", 10,
                desc: "GIGW asks a government site to tell visitors how current a page is. " +
                      "The site-wide date in the footer is separate and stays either way."),

            S("assistant.title", "Ask about the scheme", "Assistant heading", "Contact", order: 31),

            // The wording on the form itself. Government forms are often worded to a
            // standard, and a label that cannot be changed without a deploy is a label
            // that will be wrong for somebody.
            S("contact.labelName", "Your name", "Field: name", "Contact", order: 40),
            S("contact.labelEmail", "E-mail address", "Field: e-mail", "Contact", order: 41),
            S("contact.labelPhone", "Mobile number", "Field: mobile", "Contact", order: 42),
            S("contact.labelUdyam", "Udyam Registration Number", "Field: Udyam number", "Contact", order: 43),
            S("contact.labelOrganisation", "Enterprise / organisation", "Field: organisation", "Contact", order: 44),
            S("contact.labelCategory", "What is your enquiry about?", "Field: category", "Contact", order: 45),
            S("contact.udyamHint", "Helps us look up your enterprise faster.", "Field hint: Udyam", "Contact", order: 46),
            S("contact.submitLabel", "Send enquiry", "Submit button", "Contact", order: 47),
            S("contact.successTitle", "Your enquiry has been received.", "Message after sending", "Contact", order: 48),
            S("contact.privacyNote", "Your details are used only to respond to this enquiry.", "Privacy note under the form", "Contact", order: 49),

            // Who is answerable for the content of this site. GIGW requires it to be
            // published; it is seeded empty on purpose, because the block stays hidden
            // until the ministry names a real officer, and a placeholder name here
            // would read to a visitor as somebody they could write to.
            S("contact.wimTitle", "Web Information Manager", "Heading: web information manager", "Contact", order: 50),
            S("contact.wimName", "", "Web Information Manager: name", "Contact", order: 51),
            S("contact.wimDesignation", "", "Web Information Manager: designation", "Contact", order: 52),
            S("contact.wimEmail", "", "Web Information Manager: e-mail", "Contact", "email", 53),
            S("contact.wimPhone", "", "Web Information Manager: telephone", "Contact", order: 54),
            S("contact.wimAddress", "", "Web Information Manager: address", "Contact", order: 55),

            // The captions in the panel beside the form, and the words on the buttons
            // under each agency.
            S("contact.helplineNote", "Toll-free scheme helpline", "Caption: helpline", "Contact", order: 50),
            S("contact.phoneNote", "Office", "Caption: office telephone", "Contact", order: 51),
            S("contact.emailNote", "General enquiries", "Caption: general e-mail", "Contact", order: 52),
            S("contact.supportNote", "Portal and technical", "Caption: support e-mail", "Contact", order: 53),
            S("contact.mapLinkLabel", "Open in Google Maps", "Map link", "Contact", order: 54),
            S("contact.agencyWebsiteLabel", "Visit website", "Agency button: website", "Contact", order: 55),
            S("contact.agencyContactLabel", "Contact them", "Agency button: contact", "Contact", order: 56),
            // Public on purpose. The site itself has to read this to close, and marking
            // it private was why the switch did nothing: it saved, but never reached the
            // browser. Nothing is given away - a closed site announces itself anyway.
            S("feature.maintenanceMode", "false", "Maintenance mode", "Features", "boolean", 10,
                desc: "When enabled the public site shows a maintenance notice instead of content. " +
                      "The admin console stays open, and a signed-in operator still sees the site."),

            // --- SEO / analytics ------------------------------------------------------
            S("seo.defaultTitle", "MSME Competitive (LEAN) Scheme | Ministry of MSME, Government of India",
                "Default page title", "SEO", order: 1),
            S("seo.defaultDescription",
                "The MSME Competitive (LEAN) Scheme helps Indian MSMEs raise productivity, quality and " +
                "competitiveness through Lean tools and techniques, with up to 90% subsidy on consultant fees.",
                "Default meta description", "SEO", "textarea", 2),
            S("seo.defaultKeywords", "LEAN, MSME, Ministry of MSME, lean manufacturing, kaizen, competitiveness, Udyam",
                "Default meta keywords", "SEO", order: 3),
            S("analytics.gaMeasurementId", "", "Google Analytics measurement ID", "SEO", order: 4, pub: false),

            // --- FAQ page -------------------------------------------------------------
            S("faqs.helpTitle", "Still have a question?", "Panel heading", "Sidebar", order: 15),
            S("faqs.helpText", "Write to the LEAN Scheme team and we will get back to you.",
                "Panel text", "Sidebar", "textarea", 16),
            S("faqs.helpCtaText", "Contact us", "Button label", "Sidebar", order: 17),
            S("faqs.helpCtaUrl", "/contact-us", "Button link", "Sidebar", "url", 18),

            // --- Inner-page sidebar ---------------------------------------------------
            // Furniture that appears beside every content page, so it belongs to the
            // site rather than to any one page. Each panel has its own switch, and the
            // rail has one of its own, so a panel can go without the others going too.
            S("feature.sidebarWidgets", "true", "Show the sidebar rail", "Sidebar", "boolean", 1,
                desc: "Turns off all three panels at once. Content pages then run full width."),

            S("feature.sidebarDocuments", "true", "Show the key documents panel", "Sidebar", "boolean", 2,
                desc: "Lists the three most recent published documents."),
            S("sidebar.documentsTitle", "Key documents", "Panel heading", "Sidebar", order: 3),

            S("feature.sidebarHelp", "true", "Show the help panel", "Sidebar", "boolean", 4),
            S("sidebar.helpTitle", "Need help with the scheme?", "Panel heading", "Sidebar", order: 5),
            S("sidebar.helpText",
                "Our team can guide you through eligibility, registration and the handholding process.",
                "Panel text", "Sidebar", "textarea", 6),
            S("sidebar.helpCtaText", "Request a call back", "Button label", "Sidebar", order: 7),
            S("sidebar.helpCtaUrl", "/contact-us", "Button link", "Sidebar", "url", 8),

            S("feature.sidebarApply", "true", "Show the apply panel", "Sidebar", "boolean", 9),
            S("sidebar.applyTitle", "Ready to apply?", "Panel heading", "Sidebar", order: 10),
            S("sidebar.applyText", "Registration is free and needs only your Udyam Registration Number.",
                "Panel text", "Sidebar", "textarea", 11),
            S("sidebar.applyCtaText", "Apply for the LEAN Scheme", "Button label", "Sidebar", order: 12),
            S("sidebar.applyLinkText", "See how registration works", "Secondary link", "Sidebar", order: 13),
            S("sidebar.applyLinkUrl", "/register/how-to-register", "Secondary link URL",
                "Sidebar", "url", 14),

            // --- Footer ---------------------------------------------------------------
            S("footer.copyright",
                "Content owned and maintained by the Ministry of Micro, Small and Medium Enterprises, Government of India.",
                "Copyright line", "Footer", "textarea", 1),
            S("footer.disclaimer",
                "This is the official portal of the MSME Competitive (LEAN) Scheme. Information is provided by " +
                "the Ministry of MSME and updated by its implementing agencies.",
                "Footer disclaimer", "Footer", "textarea", 2),
            S("footer.lastUpdated", DateTimeOffset.UtcNow.ToString("dd MMM yyyy"), "Last updated on", "Footer", order: 3),
            S("footer.quickLinksTitle", "Quick Links", "Quick links column heading", "Footer", order: 4),
            S("footer.usefulLinksTitle", "Useful Links", "Useful links column heading", "Footer", order: 5),
            S("footer.policiesTitle", "Website Policies", "Policies column heading", "Footer", order: 6),

            // --- Statistics -----------------------------------------------------------
            S("stats.visitorCount", "0", "Visitor count", "Footer", "number", 9,
                desc: "Running total shown in the footer. Editable, so a figure carried over " +
                      "from a previous site can be seeded here.")
        ];

        var existing = await db.SiteSettings.ToDictionaryAsync(s => s.Key, ct);
        var added = 0;

        foreach (var setting in defaults)
        {
            if (!existing.TryGetValue(setting.Key, out var row))
            {
                db.SiteSettings.Add(setting);
                added++;
                continue;
            }

            row.DisplayName = setting.DisplayName;
            row.Description = setting.Description;
            row.Group = setting.Group;
            row.Section = setting.Section;
            row.DataType = setting.DataType;
            row.SortOrder = setting.SortOrder;
            row.IsPublic = setting.IsPublic;
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Site settings up to date ({Added} added).", added);
        }

        await RetireObsoleteSettingsAsync(db, logger, ct);
    }

    /// <summary>
    /// Deletes settings the site no longer reads.
    ///
    /// These were editable in the console and consulted by nothing: an editor
    /// changed one, the save succeeded, and the site did not move, with nothing to
    /// say why. The transactional addresses duplicated the login tiles, which hold
    /// their own URLs and are the place they are actually edited; the portal links
    /// duplicated the Useful Links menu; and the ministry note belonged to a contact
    /// form option that no longer exists.
    ///
    /// Removing them from the defaults above is not enough on its own - an existing
    /// database keeps the rows, and the console keeps offering them.
    /// </summary>
    private static async Task RetireObsoleteSettingsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        string[] retired =
        [
            "app.msmeLogin", "app.agencyLogin", "app.ministryLogin", "app.dfoLogin",
            "app.consultantLogin", "app.consultantRegister", "app.oemRegister",
            "app.oemLogin", "app.associationRegister", "app.associationLogin",
            "links.udyam", "links.zed", "links.innovative", "links.leanLms",
            "links.qci", "links.npc", "links.launchVideo",
            "contact.ministryNote",
            // A second address for the ministry, beside the one the header logo
            // already opens; a short site name and a short ministry name that no
            // screen has ever had room to use.
            "links.msmeMinistry", "site.shortName", "site.ministryShort",
        ];

        var rows = await db.SiteSettings.Where(x => retired.Contains(x.Key)).ToListAsync(ct);
        if (rows.Count == 0) return;

        db.SiteSettings.RemoveRange(rows);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Retired {Count} settings the site no longer reads: {Keys}",
            rows.Count, string.Join(", ", rows.Select(r => r.Key)));
    }
}
