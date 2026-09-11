using System.Text.Json;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Services.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// The grievance matrix QCI uses for SAMAR, with SAMAR read as LEAN. A starting
    /// point to be confirmed, not a decision: the values must match the options of
    /// the custom fields in the Zoho Desk department LEAN is given, which only QCI
    /// can say. Edited under Helpdesk in the console.
    /// </summary>
    public static readonly object DefaultGrievanceMatrix = new
    {
        labels = new[] { "User type", "Complaint or query", "Related to", "Specific issue" },
        options = new object[]
        {
            new
            {
                name = "MSME",
                children = new object[]
                {
                    new
                    {
                        name = "Complaint",
                        children = new object[]
                        {
                            new
                            {
                                name = "Assessor",
                                children = new object[]
                                {
                                    new { name = "Behavioral Issue" },
                                    new { name = "Ethical Issue" },
                                    new { name = "Professional Competency" },
                                },
                            },
                            new
                            {
                                name = "Certification Process/empanelment process related",
                                children = new object[]
                                {
                                    new { name = "Operational issue" },
                                    new { name = "LEAN staff related" },
                                },
                            },
                        },
                    },
                    new
                    {
                        name = "Query",
                        children = new object[]
                        {
                            new
                            {
                                name = "LEAN",
                                children = new object[]
                                {
                                    new { name = "Assessor Training Program Related" },
                                    new { name = "Registration Related" },
                                    new { name = "LEAN Application Status related" },
                                    new { name = "Assessment Related" },
                                    new { name = "Payment Related" },
                                    new { name = "Invoice Related" },
                                    new { name = "Certification related" },
                                    new { name = "Others" },
                                },
                            },
                        },
                    },
                },
            },
        },
    };

    /// <summary>
    /// The places external services plug in, and QCI's helpdesk connection - each
    /// created once, switched off, and left for the console to fill in.
    ///
    /// Nothing identifying an account is seeded. The Zoho document supplied with
    /// this work carries SAMAR's credentials and department; those are QCI's for
    /// SAMAR, and LEAN's have to be issued for LEAN and typed into the console.
    /// </summary>
    private static async Task SeedIntegrationsAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        (string Key, IntegrationMode Mode, string Title, string Intro, string Input)[] slots =
        [
            ("certificate-verification", IntegrationMode.Off, "Verify a certificate",
                "Check that a certificate issued under the MSME Competitive (LEAN) Scheme is genuine.",
                "Certificate number"),
            ("certified-units", IntegrationMode.Off, "Certified units",
                "Enterprises certified under the MSME Competitive (LEAN) Scheme.",
                "Search by name, state or certificate number"),
            ("assistant", IntegrationMode.BuiltIn, "Ask about the scheme",
                "Answers come from what this portal publishes.",
                "Your question"),
        ];

        var existing = await db.ExternalIntegrations.Select(i => i.Key).ToListAsync(ct);
        foreach (var slot in slots.Where(s => !existing.Contains(s.Key)))
        {
            db.ExternalIntegrations.Add(new ExternalIntegration
            {
                Key = slot.Key,
                Mode = slot.Mode,
                Title = slot.Title,
                Intro = slot.Intro,
                InputLabel = slot.Input,
            });
            logger.LogInformation("Added the {Key} integration, switched off", slot.Key);
        }

        if (!await db.HelpdeskConnections.AnyAsync(ct))
        {
            var qci = await db.Partners
                .Where(p => p.Type == PartnerType.ImplementationAgency && p.ShortName == "QCI")
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(ct);

            db.HelpdeskConnections.Add(new HelpdeskConnection
            {
                IsEnabled = false,
                PartnerId = qci,
                TicketTemplate = HelpdeskDispatcher.DefaultTicketTemplate,
                GrievanceMatrix = JsonSerializer.Serialize(DefaultGrievanceMatrix,
                    new JsonSerializerOptions { WriteIndented = true }),
            });
            logger.LogInformation("Added the Zoho Desk connection for QCI, switched off");
        }

        // The menu item added for certified units had no address, so it led back to
        // the home page. It has one now. Only where it is still empty: an address an
        // editor has since set is theirs.
        var certified = await db.MenuItems
            .Where(m => m.Label == "Certified Units" && (m.Url == null || m.Url == ""))
            .ToListAsync(ct);
        foreach (var item in certified)
        {
            item.Url = "/certified-units";
            item.PageId = null;
            item.OpenInNewTab = false;
        }

        await db.SaveChangesAsync(ct);
    }
}
