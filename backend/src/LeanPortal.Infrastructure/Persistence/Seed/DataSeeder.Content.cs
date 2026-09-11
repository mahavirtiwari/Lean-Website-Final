using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    private static async Task SeedContentAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        await SeedBannersAsync(db, ct);
        await SeedFaqsAsync(db, ct);
        await SeedDocumentsAsync(db, ct);
        await SeedPostsAsync(db, ct);
        await SeedTestimonialsAsync(db, ct);
        await SeedProgrammesAsync(db, ct);
        await SeedGalleryAsync(db, ct);
        logger.LogInformation("Seeded editorial content.");
    }

    private static async Task SeedBannersAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Banners.AnyAsync(ct)) return;

        db.Banners.AddRange(
            new Banner
            {
                Eyebrow = "Ministry of MSME, Government of India",
                Title = "Make your enterprise",
                HighlightedTitle = "globally competitive",
                Subtitle =
                    "The MSME Competitive (LEAN) Scheme helps manufacturing MSMEs cut waste, raise quality and " +
                    "lower cost through Lean tools and techniques - with up to 90% subsidy on consultant fees.",
                ImageUrl = "/assets/images/banners/hero-factory.svg",
                AltText = "Workers on a modern Indian manufacturing shop floor organised using Lean principles",
                PrimaryButtonText = "Apply for the LEAN Scheme",
                PrimaryButtonUrl = "/VerifyUdyam/Register",
                SecondaryButtonText = "How to register",
                SecondaryButtonUrl = "/register/how-to-register",
                SortOrder = 1
            },
            new Banner
            {
                Eyebrow = "Bronze level is free of cost",
                Title = "Three levels.",
                HighlightedTitle = "One journey to world class.",
                Subtitle =
                    "Start with the LEAN Pledge, build the foundations at Bronze level, embed Lean practice at " +
                    "Silver level and reach world-class manufacturing at Gold level.",
                ImageUrl = "/assets/images/banners/hero-levels.svg",
                AltText = "Illustration of the Bronze, Silver and Gold levels of the LEAN Scheme",
                PrimaryButtonText = "Explore the levels",
                PrimaryButtonUrl = "/about-scheme/scheme-levels",
                SecondaryButtonText = "Financial assistance",
                SecondaryButtonUrl = "/about-scheme/financial-assistance",
                SortOrder = 2
            },
            new Banner
            {
                Eyebrow = "Delivered nation-wide by QCI and NPC",
                Title = "Expert handholding,",
                HighlightedTitle = "90% subsidised",
                Subtitle =
                    "Consultants from organisations empanelled by the Quality Council of India and the National " +
                    "Productivity Council work on your shop floor to deliver measurable, sustained improvement.",
                ImageUrl = "/assets/images/banners/hero-handholding.svg",
                AltText = "A Lean consultant guiding an MSME team through a value stream mapping exercise",
                PrimaryButtonText = "About the scheme",
                PrimaryButtonUrl = "/about-scheme",
                SecondaryButtonText = "Implementation agencies",
                SecondaryButtonUrl = "/implementation-agency",
                SortOrder = 3
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedFaqsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Faqs.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;
        var order = 0;

        Faq F(string question, string answer, string category, bool featured = false) => new()
        {
            Question = question,
            Answer = answer,
            Category = category,
            SortOrder = ++order,
            IsFeatured = featured,
            Status = PublishStatus.Published,
            PublishedAt = now
        };

        db.Faqs.AddRange(
            F("What is the MSME Competitive (LEAN) Scheme?",
                "<p>The MSME Competitive (LEAN) Scheme is a Government initiative launched by the Ministry of Micro, " +
                "Small and Medium Enterprises to enhance the competitiveness of micro, small and medium enterprises " +
                "in the country. The programme implements Lean manufacturing techniques and provides for capacity " +
                "building and training of MSMEs in Lean manufacturing techniques through a network of Lean " +
                "manufacturing groups and experts.</p>",
                "About the scheme", featured: true),

            F("What is the objective of the scheme?",
                "<p>The primary objective is to enhance MSME competitiveness through Lean adoption. Specific goals " +
                "include improving the productivity and efficiency of MSMEs through the implementation of Lean " +
                "manufacturing techniques, reducing waste and improving quality, offering financial assistance, " +
                "providing capacity building, creating trained professionals for technical support, and encouraging " +
                "the adoption of best practices for growth and development in global markets.</p>",
                "About the scheme", featured: true),

            F("Is the scheme mandatory for MSMEs?",
                "<p>No. The MSME Competitive (LEAN) Scheme is not mandated by the Government; it is a voluntary " +
                "scheme which provides MSMEs a roadmap to global competitiveness.</p>",
                "About the scheme"),

            F("Who can apply for the scheme?",
                "<p>All manufacturing sector MSMEs registered with the UDYAM registration portal of the Ministry of " +
                "MSME are eligible to participate in the MSME Competitive (LEAN) Scheme.</p>",
                "Eligibility", featured: true),

            F("How do I apply for the scheme?",
                "<p>Visit this portal and click the registration tab. You will be asked to enter your Udyam " +
                "registration number and the mobile number associated with it. After validation you must read and " +
                "take the LEAN Pledge and accept the Undertaking. Your LEAN ID and password are then sent to your " +
                "registered e-mail address.</p>",
                "Registration", featured: true),

            F("From where can a unit download documents related to the scheme?",
                "<p>All scheme documents - guidelines, brochures, circulars, formats and presentations - are " +
                "available in the <a href=\"/downloads\">Downloads</a> section of this portal.</p>",
                "Registration"),

            F("Who is going to implement the scheme?",
                "<p>The MSME Competitive (LEAN) Scheme is implemented nation-wide through the implementing agencies " +
                "- the Quality Council of India (QCI) and the National Productivity Council (NPC) - on behalf of " +
                "the Ministry of MSME.</p>",
                "Implementation"),

            F("What are the major activities involved in the scheme?",
                "<p>The scheme comprises six major activities:</p>" +
                "<ol>" +
                "<li><strong>Industry Awareness Programmes / Workshops</strong> - MSMEs will be made aware of the " +
                "scheme through nation-wide awareness programmes, online and/or face to face as appropriate.</li>" +
                "<li><strong>Training Programmes</strong> - stakeholders such as MSME officers, assessors and " +
                "consultants will be trained on the scheme.</li>" +
                "<li><strong>Handholding</strong> - MSMEs will be provided handholding towards the implementation " +
                "of Lean tools and techniques at three different levels: Bronze, Silver and Gold.</li>" +
                "<li><strong>Benefits / Incentives</strong> - graded incentives will be announced by the Ministry " +
                "of MSME to encourage participation.</li>" +
                "<li><strong>PR Campaign, Advertising and Brand Promotion</strong> - nation-wide publicity will be " +
                "carried out to popularise the scheme.</li>" +
                "<li><strong>Digital Platform</strong> - the scheme process is e-enabled through a single-window " +
                "digital platform.</li>" +
                "</ol>",
                "About the scheme"),

            F("What is the fee structure for the different levels of the scheme?",
                "<ul>" +
                "<li><strong>Bronze</strong> - free of cost.</li>" +
                "<li><strong>Silver</strong> - 90% subsidy on the implementation cost of the consultant fees.</li>" +
                "<li><strong>Gold</strong> - 90% subsidy on the implementation cost of the consultant fees.</li>" +
                "</ul>",
                "Fees and subsidy", featured: true),

            F("Is there any additional subsidy available?",
                "<p>Yes. The Ministry of MSME provides a subsidy of 90% to micro, small and medium enterprises. " +
                "There is an additional subsidy of 5% for MSMEs owned by Women, SC and ST entrepreneurs, and " +
                "&#8377; 5,000 per MSME is given to the OEM or Association after the MSME completes all stages of " +
                "Lean intervention.</p>",
                "Fees and subsidy"),

            F("Who conducts the handholding activity?",
                "<p>The handholding activity is allocated to consulting organisations empanelled by the Quality " +
                "Council of India and the National Productivity Council. The handholding is carried out by " +
                "consultants from these empanelled organisations.</p>",
                "Implementation"),

            F("Can MSMEs choose their own consultants or assessors?",
                "<p>No. The allocation is done by the implementing agency through a pre-defined process. MSMEs may " +
                "provide a list of preferred consultant organisations to the implementing agency. Selection uses a " +
                "financial and technical bidding process in which technical proficiency carries 70% weightage and " +
                "the financial bid carries 30% weightage.</p>",
                "Implementation"),

            F("How can I access the detailed scheme guidelines?",
                "<p>The approved scheme guidelines are published in the <a href=\"/downloads\">Downloads</a> " +
                "section of this portal and can be downloaded free of charge.</p>",
                "About the scheme"),

            F("Does the scheme cover all MSME sectors?",
                "<p>Yes. The MSME Competitive (LEAN) Scheme covers all MSME sectors belonging to manufacturing.</p>",
                "Eligibility"),

            F("Is the scheme applicable to MSMEs in the service sector?",
                "<p>At present the scheme is designed for the manufacturing MSMEs of India. The second phase of the " +
                "scheme will be opened for the service sector.</p>",
                "Eligibility"),

            F("If an MSME has no Udyam registration, is it eligible to apply?",
                "<p>No. Only MSMEs which hold a UDYAM registration number are eligible. If you do not have one, " +
                "register free of cost on the <a href=\"https://udyamregistration.gov.in/\" target=\"_blank\" " +
                "rel=\"noopener noreferrer\">Udyam Registration portal</a> and then return to this portal.</p>",
                "Eligibility"),

            F("Are unregistered MSMEs eligible to participate?",
                "<p>No. Only UDYAM registered MSMEs can participate in this scheme.</p>",
                "Eligibility"),

            F("Which states and union territories can participate?",
                "<p>The scheme is applicable to all states and union territories of India.</p>",
                "Eligibility"),

            F("What is the LEAN Pledge and why is it required?",
                "<p>Every MSME that embarks on the journey of Lean - Bronze, Silver or Gold - has to take " +
                "a LEAN Pledge. The intent of the pledge is to make a pre-commitment, a solemn promise by the MSME " +
                "to uphold the values of Lean practices and philosophy in its functioning. After taking the pledge " +
                "the unit can apply for handholding depending on its need, level of preparedness and interest.</p>",
                "Registration"),

            F("What certification do I receive on completing a level?",
                "<p>An E-Certificate towards participation under the scheme is issued by the Ministry of MSME after " +
                "completion of the Bronze, Silver and Gold levels. The list of MSMEs that have taken the " +
                "LEAN Pledge and achieved any Lean implementation level is displayed on this portal.</p>",
                "Certification")
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDocumentsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Documents.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;

        db.Documents.AddRange(
            new DocumentItem
            {
                Title = "MSME Competitive (LEAN) Scheme - Approved Guidelines",
                Description =
                    "The complete approved scheme guidelines. Explore the LEAN guidelines to ensure proper " +
                    "implementation of the scheme at your enterprise.",
                Category = DocumentCategory.SchemeGuideline,
                FileUrl = "/uploads/documents/lean-scheme-guidelines.pdf",
                FileType = "PDF",
                FileSizeBytes = 2_411_724,
                DocumentDate = new DateTimeOffset(2022, 10, 7, 0, 0, 0, TimeSpan.Zero),
                Version = "07-10-2022",
                IsFeatured = true,
                SortOrder = 1,
                Status = PublishStatus.Published,
                PublishedAt = now
            },
            new DocumentItem
            {
                Title = "LEAN Scheme Brochure",
                Description =
                    "Details of the LEAN Scheme, Lean concepts, scheme levels, benefits and the registration " +
                    "process in a single printable catalogue.",
                Category = DocumentCategory.Brochure,
                FileUrl = "/uploads/documents/lean-scheme-brochure.pdf",
                FileType = "PDF",
                FileSizeBytes = 4_186_112,
                IsFeatured = true,
                SortOrder = 2,
                Status = PublishStatus.Published,
                PublishedAt = now
            },
            new DocumentItem
            {
                Title = "Registration Process Presentation",
                Description = "Step-by-step walkthrough of the eight-step LEAN Scheme registration process.",
                Category = DocumentCategory.Presentation,
                FileUrl = "/uploads/documents/lean-registration-process.pdf",
                FileType = "PDF",
                FileSizeBytes = 1_874_432,
                IsFeatured = true,
                SortOrder = 3,
                Status = PublishStatus.Published,
                PublishedAt = now
            },
            new DocumentItem
            {
                Title = "LEAN Pledge and Undertaking Format",
                Description = "The pledge and undertaking that every participating MSME accepts before handholding begins.",
                Category = DocumentCategory.Format,
                FileUrl = "/uploads/documents/lean-pledge-undertaking.pdf",
                FileType = "PDF",
                FileSizeBytes = 328_704,
                SortOrder = 4,
                Status = PublishStatus.Published,
                PublishedAt = now
            },
            new DocumentItem
            {
                Title = "Consultant Organisation Empanelment Guidelines",
                Description = "Eligibility, evaluation criteria and the application process for consultant organisations.",
                Category = DocumentCategory.SchemeGuideline,
                FileUrl = "/uploads/documents/consultant-empanelment-guidelines.pdf",
                FileType = "PDF",
                FileSizeBytes = 962_560,
                SortOrder = 5,
                Status = PublishStatus.Published,
                PublishedAt = now
            },
            new DocumentItem
            {
                Title = "Assessment Checklist - Bronze Level",
                Description = "The assessment checklist used by empanelled assessors at the Bronze level.",
                Category = DocumentCategory.Format,
                FileUrl = "/uploads/documents/assessment-checklist-basic.xlsx",
                FileType = "XLSX",
                FileSizeBytes = 156_672,
                SortOrder = 6,
                Status = PublishStatus.Published,
                PublishedAt = now
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPostsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Posts.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;

        db.Posts.AddRange(
            new Post
            {
                Type = PostType.Announcement,
                Slug = "lean-scheme-registration-open-all-states",
                Title = "LEAN Scheme registration is open to manufacturing MSMEs in all states and union territories",
                Excerpt =
                    "Manufacturing MSMEs holding a Udyam Registration Number can register on the portal, take the " +
                    "LEAN Pledge and begin the Bronze level free of cost.",
                Body =
                    "<p>Registration under the MSME Competitive (LEAN) Scheme is open to all manufacturing MSMEs " +
                    "registered on the Udyam portal, across every state and union territory of India.</p>" +
                    "<p>Enterprises can register with their Udyam Registration Number and the mobile number linked " +
                    "to it, nominate a LEAN Coordinator, accept the Undertaking and take the LEAN Pledge. The Bronze " +
                    "level, including access to the LEAN Learning Management System, is free of cost.</p>",
                IsFeatured = true,
                ShowInTicker = true,
                Status = PublishStatus.Published,
                PublishedAt = now.AddDays(-6)
            },
            new Post
            {
                Type = PostType.News,
                Slug = "awareness-programmes-scheduled-across-india",
                Title = "Fresh schedule of LEAN awareness programmes announced by QCI and NPC",
                Excerpt =
                    "A new calendar of awareness programmes and workshops has been published for MSMEs across " +
                    "multiple states, delivered both online and face to face.",
                Body =
                    "<p>The implementing agencies have published a fresh calendar of industry awareness programmes " +
                    "under the MSME Competitive (LEAN) Scheme.</p>" +
                    "<p>The sessions explain the objectives of the scheme, the three implementation levels, the " +
                    "financial assistance available and the registration process. Participation is free and open to " +
                    "all manufacturing MSMEs. Check the " +
                    "<a href=\"/programmes/awareness\">Awareness Programmes</a> listing for sessions in your state.</p>",
                ShowInTicker = true,
                Status = PublishStatus.Published,
                PublishedAt = now.AddDays(-13)
            },
            new Post
            {
                Type = PostType.Circular,
                Slug = "circular-additional-incentive-women-sc-st-entrepreneurs",
                Title = "Circular: additional 5% assistance for enterprises owned by Women, SC and ST entrepreneurs",
                Excerpt =
                    "MSMEs owned by Women, SC and ST entrepreneurs receive an additional 5% subsidy over and above " +
                    "the 90% support on consultant fees.",
                Body =
                    "<p>This circular confirms that an additional subsidy of 5% is available to MSMEs owned by " +
                    "Women, SC and ST entrepreneurs, over and above the 90% subsidy on the implementation cost of " +
                    "consultant fees at the Silver and Gold levels.</p>",
                AttachmentUrl = "/uploads/documents/circular-additional-incentive.pdf",
                AttachmentLabel = "Download circular (PDF)",
                ShowInTicker = true,
                Status = PublishStatus.Published,
                PublishedAt = now.AddDays(-21)
            },
            new Post
            {
                Type = PostType.News,
                Slug = "lean-lms-basic-level-modules-refreshed",
                Title = "LEAN LMS Bronze level e-modules refreshed with new case studies",
                Excerpt =
                    "The Learning Management System used for the Bronze level now includes updated Indian MSME case " +
                    "studies on 5S, waste identification and visual management.",
                Body =
                    "<p>The LEAN Learning Management System, which delivers the Bronze level curriculum, has been " +
                    "updated with new modules and Indian MSME case studies covering 5S workplace organisation, " +
                    "identification of the seven wastes and visual management on the shop floor.</p>",
                Status = PublishStatus.Published,
                PublishedAt = now.AddDays(-33)
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedTestimonialsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Testimonials.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;

        db.Testimonials.AddRange(
            new Testimonial
            {
                UnitName = "Precision auto components unit, Pune",
                PersonName = "Managing Director",
                Sector = "Auto components",
                Location = "Pune, Maharashtra",
                Quote =
                    "We began with 5S and visual management on one line. Within two quarters our rejection rate had " +
                    "fallen sharply, the shop floor was visibly calmer, and we freed up enough space to add a cell " +
                    "without taking new premises. The team now raises kaizens on its own.",
                ImpactHighlight = "Rejections down, floor space released for a new cell",
                AchievedLevel = LeanLevel.Silver,
                SortOrder = 1,
                Status = PublishStatus.Published,
                PublishedAt = now
            },
            new Testimonial
            {
                UnitName = "Sheet metal fabrication unit, Coimbatore",
                PersonName = "Partner",
                Sector = "Sheet metal fabrication",
                Location = "Coimbatore, Tamil Nadu",
                Quote =
                    "The consultant did not hand us a report and leave. They worked on the floor with our " +
                    "supervisors through value stream mapping and quick changeover. Setup time on our press line " +
                    "dropped substantially and we now quote shorter delivery periods with confidence.",
                ImpactHighlight = "Shorter setup times and committed delivery periods",
                AchievedLevel = LeanLevel.Gold,
                SortOrder = 2,
                Status = PublishStatus.Published,
                PublishedAt = now
            }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedProgrammesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.AwarenessProgrammes.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;
        var seq = 0;

        AwarenessProgramme A(string title, string type, string agency, string state, string district,
            string venue, int daysFromNow, int registered, ProgrammeStatus status) => new()
        {
            ProgrammeCode = $"LEAN/{DateTime.UtcNow:yyyy}/{++seq:D4}",
            Title = title,
            ProgrammeType = type,
            Agency = agency,
            State = state,
            District = district,
            Venue = venue,
            StartDate = now.AddDays(daysFromNow),
            EndDate = now.AddDays(daysFromNow),
            RegisteredCount = registered,
            Capacity = 250,
            ProgrammeStatus = status,
            Description =
                "An in-depth session on the MSME Competitive (LEAN) Scheme covering its objectives, the three " +
                "implementation levels, the financial assistance available and the registration process.",
            Status = PublishStatus.Published,
            PublishedAt = now
        };

        db.AwarenessProgrammes.AddRange(
            A("LEAN Scheme Awareness Programme for MSMEs", "Awareness Programme", "QCI",
                "Maharashtra", "Pune", "MCCIA Trade Tower, Senapati Bapat Road, Pune", 14, 96, ProgrammeStatus.RegistrationOpen),
            A("LEAN Scheme Awareness Programme for MSMEs", "Awareness Programme", "NPC",
                "Tamil Nadu", "Coimbatore", "CODISSIA Trade Fair Complex, Coimbatore", 21, 64, ProgrammeStatus.RegistrationOpen),
            A("LEAN Scheme Awareness Programme for MSMEs", "Awareness Programme", "QCI",
                "Gujarat", "Rajkot", "Rajkot Engineering Association, Rajkot", 28, 41, ProgrammeStatus.Upcoming),
            A("Training Programme for LEAN Assessors", "Assessor Training", "QCI",
                "Delhi", "New Delhi", "Institution of Engineers, Bahadur Shah Zafar Marg, New Delhi", 35, 28, ProgrammeStatus.Upcoming),
            A("Training Programme for LEAN Consultants", "Consultant Training", "NPC",
                "Karnataka", "Bengaluru", "National Productivity Council Regional Office, Bengaluru", 42, 19, ProgrammeStatus.Upcoming),
            A("LEAN Scheme Awareness Programme for MSMEs", "Awareness Programme", "NPC",
                "Uttar Pradesh", "Kanpur", "IIA Bhawan, Kanpur", -18, 233, ProgrammeStatus.Completed),
            A("LEAN Scheme Awareness Programme for MSMEs", "Awareness Programme", "QCI",
                "West Bengal", "Howrah", "Bengal Chamber of Commerce, Howrah", -32, 147, ProgrammeStatus.Completed),
            A("Training Programme for LEAN Assessors", "Assessor Training", "NPC",
                "Rajasthan", "Jaipur", "RIICO Industrial Area, Jaipur", -47, 52, ProgrammeStatus.Completed)
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedGalleryAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.GalleryAlbums.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;

        var launch = new GalleryAlbum
        {
            Slug = "scheme-launch",
            Title = "Launch of the MSME Competitive (LEAN) Scheme",
            Description = "Photographs from the launch of the scheme by the Ministry of MSME.",
            Location = "New Delhi",
            EventDate = new DateTimeOffset(2023, 3, 10, 0, 0, 0, TimeSpan.Zero),
            CoverImageUrl = "/assets/images/gallery/launch/cover.svg",
            SortOrder = 1,
            Status = PublishStatus.Published,
            PublishedAt = now
        };
        launch.Images =
        [
            new GalleryImage { ImageUrl = "/assets/images/gallery/launch/01.svg", AltText = "Inauguration of the LEAN Scheme", Caption = "Inauguration ceremony", SortOrder = 1 },
            new GalleryImage { ImageUrl = "/assets/images/gallery/launch/02.svg", AltText = "Panel discussion at the launch event", Caption = "Panel discussion on MSME competitiveness", SortOrder = 2 },
            new GalleryImage { ImageUrl = "/assets/images/gallery/launch/03.svg", AltText = "Delegates at the LEAN Scheme launch", Caption = "Delegates from industry associations", SortOrder = 3 }
        ];

        var workshops = new GalleryAlbum
        {
            Slug = "awareness-workshops",
            Title = "Awareness programmes and workshops",
            Description = "Awareness programmes delivered for MSMEs across states and districts.",
            EventDate = now.AddMonths(-3),
            CoverImageUrl = "/assets/images/gallery/workshops/cover.svg",
            SortOrder = 2,
            Status = PublishStatus.Published,
            PublishedAt = now
        };
        workshops.Images =
        [
            new GalleryImage { ImageUrl = "/assets/images/gallery/workshops/01.svg", AltText = "MSME representatives at an awareness workshop", Caption = "Awareness workshop, Pune", SortOrder = 1 },
            new GalleryImage { ImageUrl = "/assets/images/gallery/workshops/02.svg", AltText = "Trainer presenting Lean concepts", Caption = "Session on Lean tools and techniques", SortOrder = 2 },
            new GalleryImage { ImageUrl = "/assets/images/gallery/workshops/03.svg", AltText = "Participants during a group exercise", Caption = "Group exercise on waste identification", SortOrder = 3 }
        ];

        var shopFloor = new GalleryAlbum
        {
            Slug = "lean-on-the-shop-floor",
            Title = "LEAN on the shop floor",
            Description = "Before and after views from MSME units that completed Lean implementation.",
            EventDate = now.AddMonths(-6),
            CoverImageUrl = "/assets/images/gallery/shopfloor/cover.svg",
            SortOrder = 3,
            Status = PublishStatus.Published,
            PublishedAt = now
        };
        shopFloor.Images =
        [
            new GalleryImage { ImageUrl = "/assets/images/gallery/shopfloor/01.svg", AltText = "Shop floor organised using 5S", Caption = "5S implementation on a machining line", SortOrder = 1 },
            new GalleryImage { ImageUrl = "/assets/images/gallery/shopfloor/02.svg", AltText = "Visual management board on a shop floor", Caption = "Daily visual management board", SortOrder = 2 },
            new GalleryImage { ImageUrl = "/assets/images/gallery/shopfloor/03.svg", AltText = "Reorganised material storage area", Caption = "Reorganised material storage", SortOrder = 3 }
        ];

        db.GalleryAlbums.AddRange(launch, workshops, shopFloor);
        await db.SaveChangesAsync(ct);
    }
}
