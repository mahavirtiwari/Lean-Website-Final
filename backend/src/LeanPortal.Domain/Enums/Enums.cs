namespace LeanPortal.Domain.Enums;

/// <summary>Where a menu item is rendered on the public site.</summary>
public enum MenuLocation
{
    TopBar = 0,
    Main = 1,
    Footer = 2,
    QuickLinks = 3,
    UsefulLinks = 4,
    /// <summary>The strip at the very bottom of every page: Sitemap, Screen Reader Access, Disclaimer.</summary>
    FooterBottom = 5
}

public enum PostType
{
    News = 0,
    Announcement = 1,
    PressRelease = 2,
    Circular = 3,
    Tender = 4,
    SuccessStory = 5
}

public enum DocumentCategory
{
    SchemeGuideline = 0,
    Brochure = 1,
    Circular = 2,
    Format = 3,
    Presentation = 4,
    Report = 5,
    Policy = 6,
    Other = 7
}

/// <summary>The three LEAN implementation tiers (plus the pre-commitment pledge).</summary>
public enum LeanLevel
{
    Pledge = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}

public enum PartnerType
{
    ImplementationAgency = 0,
    IndustryAssociation = 1,
    Oem = 2,
    ConsultantOrganization = 3,
    UsefulLink = 4
}

public enum ProgrammeStatus
{
    Upcoming = 0,
    RegistrationOpen = 1,
    Completed = 2,
    Cancelled = 3
}

public enum ContactMessageStatus
{
    New = 0,
    InProgress = 1,
    Responded = 2,
    Closed = 3,
    Spam = 4
}

/// <summary>Layout template used to render a CMS page on the public site.</summary>
public enum PageTemplate
{
    /// <summary>Inner page with a left sidebar (finance-press style). Default.</summary>
    SidebarLeft = 0,
    /// <summary>Inner page, content full width under the banner.</summary>
    FullWidth = 1,
    /// <summary>Composed of PageBlocks - used by the home page.</summary>
    Blocks = 2,
    /// <summary>Page rendered by a dedicated Angular component (gallery, contact, FAQs...).</summary>
    Custom = 3
}

/// <summary>Structured block types available to the home page / landing page builder.</summary>
public enum BlockType
{
    HeroSlider = 0,
    QuickActionCards = 1,
    WelcomeVideo = 2,
    StatisticsCounter = 3,
    SchemeComponentsGrid = 4,
    DocumentsNotices = 5,
    MinisterMessage = 6,
    SchemeLevels = 7,
    LoginPortals = 8,
    Initiatives = 9,
    Testimonials = 10,
    UsefulLinks = 11,
    CallToAction = 12,
    ContactStrip = 13,
    RichText = 14,
    GalleryShowcase = 15,
    PartnersStrip = 16,
    BenefitsIncentives = 17
}

/// <summary>Which body offers an incentive, and so which tab it is listed under.</summary>
public enum IncentiveCategory
{
    Ministry = 0,
    States = 1,
    Financial = 2,
    Other = 3
}

/// <summary>How a part of the portal fed from outside gets its content.</summary>
public enum IntegrationMode
{
    /// <summary>Not offered yet. The page says so and points to the contact form.</summary>
    Off = 0,
    /// <summary>A button to the provider's own page, opened in a new tab.</summary>
    Link = 1,
    /// <summary>The provider's page, shown in a frame on ours.</summary>
    Frame = 2,
    /// <summary>A snippet from the provider, run in a sandboxed frame.</summary>
    Embed = 3,
    /// <summary>Called from the server; the results are drawn by the portal itself.</summary>
    Api = 4,
    /// <summary>The assistant only: answers from the portal's own content.</summary>
    BuiltIn = 5
}

/// <summary>Where an enquiry stands with the agency's helpdesk.</summary>
public enum HelpdeskStatus
{
    /// <summary>The agency does not use a helpdesk; the enquiry went by e-mail.</summary>
    NotApplicable = 0,
    /// <summary>Waiting to be raised, or to be retried after a failure.</summary>
    Pending = 1,
    Sent = 2,
    /// <summary>Retried until the schedule ran out. Sent to the agency's inbox instead.</summary>
    Failed = 3
}
