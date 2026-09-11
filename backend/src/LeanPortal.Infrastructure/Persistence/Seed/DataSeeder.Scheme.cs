using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Scheme reference data: the three implementation levels, the six scheme components,
    /// the portal analytics counters, stakeholder login tiles and partner organisations.
    /// </summary>
    private static async Task SeedSchemeAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        await SeedSchemeLevelsAsync(db, ct);
        await SeedSchemeComponentsAsync(db, ct);
        await SeedStatisticsAsync(db, ct);
        await SeedLoginPortalsAsync(db, ct);
        await SeedPartnersAsync(db, ct);
        logger.LogInformation("Seeded scheme reference data.");
    }

    private static async Task SeedSchemeLevelsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.SchemeLevels.AnyAsync(ct)) return;

        db.SchemeLevels.AddRange(
            new SchemeLevel
            {
                Level = LeanLevel.Pledge,
                Name = "LEAN Pledge",
                BadgeLabel = "Pre-commitment",
                Tagline = "The starting point of every LEAN journey",
                Description =
                    "Every MSME that embarks on the journey of Lean (Bronze, Silver, Gold) has to take a " +
                    "LEAN Pledge. The intent of taking a Lean Pledge is to make a pre-commitment, a solemn promise by " +
                    "the MSME to uphold the values of Lean practices and philosophy in its functioning. After taking " +
                    "the pledge the unit can apply for handholding depending on its need, level of preparedness and interest.",
                Deliverables =
                    "Register with your Udyam Registration Number\n" +
                    "Nominate a LEAN Coordinator for the unit\n" +
                    "Accept the Undertaking and take the LEAN Pledge\n" +
                    "Obtain your LEAN ID and access the e-learning modules",
                FeeStructure = "No fee",
                Duration = "Same day",
                AccentColor = "#0e6e7c",
                SortOrder = 0
            },
            new SchemeLevel
            {
                Level = LeanLevel.Bronze,
                Name = "Bronze Level",
                BadgeLabel = "Bronze",
                Tagline = "Build LEAN awareness and put the foundations in place",
                Description =
                    "The Bronze level introduces the unit to core Lean tools through the LEAN Learning Management " +
                    "System and structured handholding. It focuses on workplace organisation, visual management and " +
                    "waste identification so that the enterprise can see and measure its own losses for the first time.",
                Deliverables =
                    "5S workplace organisation across the shop floor\n" +
                    "Visual management and standard operating procedures\n" +
                    "Identification of the seven wastes and quick-win kaizens\n" +
                    "Baseline measurement of productivity and quality parameters\n" +
                    "Assessment by an empanelled assessor and e-Certificate on completion",
                FeeStructure = "Free of cost",
                Duration = "Approximately 3 months",
                AccentColor = "#166b8a",
                SortOrder = 1
            },
            new SchemeLevel
            {
                Level = LeanLevel.Silver,
                Name = "Silver Level",
                BadgeLabel = "Silver",
                Tagline = "Embed LEAN into day-to-day manufacturing practice",
                Description =
                    "At the Silver level, consultants from organisations empanelled by QCI and NPC work with " +
                    "the unit to deploy the full Lean toolkit on selected value streams, converting the awareness " +
                    "built at Bronze level into measurable and sustained gains.",
                Deliverables =
                    "Value stream mapping of priority product families\n" +
                    "Total Productive Maintenance and autonomous maintenance routines\n" +
                    "Quick changeover (SMED) and layout / flow improvement\n" +
                    "Poka-yoke, quality at source and structured problem solving\n" +
                    "Documented savings in rejection, inventory, space and energy\n" +
                    "Assessment and e-Certificate issued by MoMSME",
                FeeStructure = "90% subsidy on the implementation cost of consultant fees",
                Duration = "Approximately 6 months",
                AccentColor = "#31669b",
                SortOrder = 2
            },
            new SchemeLevel
            {
                Level = LeanLevel.Gold,
                Name = "Gold Level",
                BadgeLabel = "Gold",
                Tagline = "Achieve world-class manufacturing and supply-chain readiness",
                Description =
                    "The Gold level takes the enterprise towards world-class manufacturing: sustained Lean " +
                    "culture, integration with the supply chain, digital dashboards and an introduction to " +
                    "Industry 4.0 so that the unit becomes a preferred supplier in global value chains.",
                Deliverables =
                    "Enterprise-wide Lean deployment and policy deployment (hoshin kanri)\n" +
                    "Supplier and customer integration across the value chain\n" +
                    "Digital dashboards, MIS and introduction to Industry 4.0\n" +
                    "Sustained cost, quality, delivery, safety and morale improvements\n" +
                    "Capability to enter and compete in global markets\n" +
                    "Assessment and Gold level e-Certificate issued by MoMSME",
                FeeStructure = "90% subsidy on the implementation cost of consultant fees",
                Duration = "Approximately 9 months",
                AccentColor = "#495eb2",
                SortOrder = 3
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSchemeComponentsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.SchemeComponents.AnyAsync(ct)) return;

        db.SchemeComponents.AddRange(
            new SchemeComponent
            {
                Title = "Industry Awareness Programmes / Workshops",
                ShortDescription = "Nation-wide awareness programmes, online and face to face.",
                Description =
                    "MSMEs will be made aware of the scheme through nation-wide awareness programmes conducted " +
                    "online and face to face, with the support of industry associations and government agencies.",
                Icon = "megaphone",
                LinkUrl = "/programmes/awareness",
                SortOrder = 1
            },
            new SchemeComponent
            {
                Title = "Training Programmes",
                ShortDescription = "Capacity building for MSME officers, assessors and consultants.",
                Description =
                    "Stakeholders such as MSME officers, assessors and consultants are trained on the MSME " +
                    "Competitive (LEAN) Scheme so that implementing agencies can deliver it consistently across India.",
                Icon = "graduation-cap",
                LinkUrl = "/programmes/training",
                SortOrder = 2
            },
            new SchemeComponent
            {
                Title = "Handholding",
                ShortDescription = "Implementation support at Bronze, Silver and Gold levels.",
                Description =
                    "MSMEs are provided handholding towards the implementation of Lean tools and techniques at " +
                    "three different levels - Bronze, Silver and Gold - with a verifiable assessment at each stage.",
                Icon = "handshake",
                LinkUrl = "/about-scheme/scheme-levels",
                SortOrder = 3
            },
            new SchemeComponent
            {
                Title = "Benefits and Incentives",
                ShortDescription = "Graded incentives that reward every milestone achieved.",
                Description =
                    "Graded incentives are announced by the Ministry of MSME to encourage participation, linked to " +
                    "milestones such as generating a LEAN ID, completing the LEAN Pledge and implementing Lean at each level.",
                Icon = "award",
                LinkUrl = "/about-scheme/financial-assistance",
                SortOrder = 4
            },
            new SchemeComponent
            {
                Title = "PR Campaign, Advertising and Brand Promotion",
                ShortDescription = "Nation-wide publicity to popularise the LEAN Scheme.",
                Description =
                    "For popularising the LEAN Scheme, nation-wide publicity is undertaken so that manufacturing " +
                    "MSMEs across every state and union territory learn about the support available to them.",
                Icon = "bullhorn",
                LinkUrl = "/media/news",
                SortOrder = 5
            },
            new SchemeComponent
            {
                Title = "Digital Platform",
                ShortDescription = "A single-window digital platform for the entire journey.",
                Description =
                    "The LEAN Scheme process is e-enabled through a single-window digital platform used for " +
                    "registration, pledge, handholding, assessment and issue of e-Certificates.",
                Icon = "monitor",
                LinkUrl = "/register/how-to-register",
                SortOrder = 6
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedStatisticsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Statistics.AnyAsync(ct)) return;

        // Baseline figures mirror the counters published on the existing LEAN portal.
        // In production these are refreshed from the transactional database via SourceQueryKey.
        db.Statistics.AddRange(
            new Statistic
            {
                Label = "MSMEs Registered", Value = 68704, Suffix = "+", Icon = "factory",
                LinkUrl = "/participants/registered-msmes", SourceQueryKey = "msme.registered", SortOrder = 1
            },
            new Statistic
            {
                Label = "LEAN Pledges Taken", Value = 68371, Suffix = "+", Icon = "hand-raised",
                SourceQueryKey = "msme.pledged", SortOrder = 2
            },
            new Statistic
            {
                Label = "Group of Enterprises", Value = 446, Icon = "users",
                LinkUrl = "/participants/goe", SourceQueryKey = "goe.total", SortOrder = 3
            },
            new Statistic
            {
                Label = "Bronze Level Certificates", Value = 19798, Suffix = "+", Icon = "certificate",
                SourceQueryKey = "cert.basic", SortOrder = 4
            },
            new Statistic
            {
                Label = "Silver Level Registered", Value = 4007, Suffix = "+", Icon = "trending-up",
                SourceQueryKey = "level.intermediate", SortOrder = 5
            },
            new Statistic
            {
                Label = "Consultants Registered", Value = 209, Icon = "user-check",
                LinkUrl = "/participants/consultants", SourceQueryKey = "consultant.registered", SortOrder = 6
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedLoginPortalsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.LoginPortals.AnyAsync(ct)) return;

        db.LoginPortals.AddRange(
            new LoginPortal
            {
                Title = "New MSME Units", Audience = "For MSMEs",
                Description = "Register with your Udyam Registration Number and begin your LEAN journey.",
                Icon = "factory", RegisterUrl = "/VerifyUdyam/Register", RegisterText = "Register Now",
                AccentColor = "#0e6e7c", SortOrder = 1
            },
            new LoginPortal
            {
                Title = "Registered MSME Units", Audience = "For MSMEs",
                Description = "Access your dashboard, e-modules, assessments and e-Certificates.",
                Icon = "login", LoginUrl = "/VerifyUdyam/Login", LoginText = "Login Now",
                AccentColor = "#246991", SortOrder = 2
            },
            new LoginPortal
            {
                Title = "Implementation Agency", Audience = "QCI and NPC",
                Description = "Manage handholding allocation, assessors and programme delivery.",
                Icon = "building", LoginUrl = "/AgencyLogin/AgencyLogin", LoginText = "Login Now",
                AccentColor = "#3964a3", SortOrder = 3
            },
            new LoginPortal
            {
                Title = "Ministry of MSME", Audience = "Ministry login",
                Description = "Monitor scheme progress, approvals and nation-wide analytics.",
                Icon = "shield", LoginUrl = "/www/MSMELogin.aspx", LoginText = "Login Now",
                AccentColor = "#4e5cb3", SortOrder = 4
            },
            new LoginPortal
            {
                Title = "State Login", Audience = "MSME DFO",
                Description = "Track participation and outcomes across your state or union territory.",
                Icon = "map", LoginUrl = "/www/DFOLogin.aspx", LoginText = "Login Now",
                AccentColor = "#15715a", SortOrder = 5
            },
            new LoginPortal
            {
                Title = "Consultant Organisation / Assessor", Audience = "For professionals",
                Description = "Empanelled consultants and assessors sign in to manage assignments.",
                Icon = "clipboard-check", LoginUrl = "/www/Login.aspx", LoginText = "Login Now",
                RegisterUrl = "/www/Registration/Registration.aspx", RegisterText = "Register",
                AccentColor = "#7253a8", SortOrder = 6
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPartnersAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Partners.AnyAsync(ct)) return;

        db.Partners.AddRange(
            new Partner
            {
                Type = PartnerType.ImplementationAgency,
                Name = "Quality Council of India",
                ShortName = "QCI",
                Description =
                    "QCI is the national body for quality established by the Government of India jointly with " +
                    "Indian industry. As an implementing agency for the LEAN Scheme it empanels consultant " +
                    "organisations and assessors, and delivers awareness and training programmes across India.",
                WebsiteUrl = "https://qcin.org/",
                ContactUrl = "https://ndie.qcin.org/contact-us/",
                LogoUrl = "/assets/images/partners/qci.svg",
                IsFeatured = true,
                SortOrder = 1
            },
            new Partner
            {
                Type = PartnerType.ImplementationAgency,
                Name = "National Productivity Council",
                ShortName = "NPC",
                Description =
                    "NPC is an autonomous organisation under the Department for Promotion of Industry and Internal " +
                    "Trade. As an implementing agency it provides productivity consultancy, trains assessors and " +
                    "consultants, and supports MSMEs through the handholding phases of the LEAN Scheme.",
                WebsiteUrl = "https://www.npcindia.gov.in/NPC/User/",
                ContactUrl = "https://www.npcindia.gov.in/NPC/User/ContactUs",
                LogoUrl = "/assets/images/partners/npc.svg",
                IsFeatured = true,
                SortOrder = 2
            },
            new Partner
            {
                Type = PartnerType.UsefulLink,
                Name = "Udyam Registration",
                ShortName = "Udyam",
                Description = "Online registration for micro, small and medium enterprises.",
                WebsiteUrl = "https://udyamregistration.gov.in/Government-India/Ministry-MSME-registration.htm",
                LogoUrl = "/assets/images/partners/udyam.svg",
                SortOrder = 1
            },
            new Partner
            {
                Type = PartnerType.UsefulLink,
                Name = "ZED Certification",
                ShortName = "ZED",
                Description = "Zero Defect Zero Effect certification scheme for MSMEs.",
                WebsiteUrl = "https://zed.msme.gov.in/",
                LogoUrl = "/assets/images/partners/zed.svg",
                SortOrder = 2
            },
            new Partner
            {
                Type = PartnerType.UsefulLink,
                Name = "MSME Innovative Scheme",
                ShortName = "Innovative",
                Description = "Incubation, design intervention and IPR support for MSMEs.",
                WebsiteUrl = "https://innovative.msme.gov.in/",
                LogoUrl = "/assets/images/partners/innovative.svg",
                SortOrder = 3
            },
            new Partner
            {
                Type = PartnerType.UsefulLink,
                Name = "Ministry of MSME",
                ShortName = "MoMSME",
                Description = "The parent ministry for all MSME schemes and programmes.",
                WebsiteUrl = "https://msme.gov.in/",
                LogoUrl = "/assets/images/partners/msme.svg",
                SortOrder = 4
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
