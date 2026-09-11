using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Application.Contracts;

// ---------------------------------------------------------------- navigation ----

public record MenuItemDto(
    int Id,
    string Label,
    string? Url,
    string? Slug,
    string? Icon,
    bool OpenInNewTab,
    bool IsHighlighted,
    IReadOnlyList<MenuItemDto> Children);

public record NavigationDto(
    IReadOnlyList<MenuItemDto> TopBar,
    IReadOnlyList<MenuItemDto> Main,
    IReadOnlyList<MenuItemDto> Footer,
    IReadOnlyList<MenuItemDto> QuickLinks,
    IReadOnlyList<MenuItemDto> UsefulLinks,
    IReadOnlyList<MenuItemDto> FooterBottom);

// --------------------------------------------------------------------- pages ----

public record BreadcrumbDto(string Label, string? Url);

public record PageSummaryDto(int Id, string Slug, string Title, string? ShortTitle, string? Summary, int SortOrder);

public record PageDto(
    int Id,
    string Slug,
    string Title,
    string? ShortTitle,
    string? Summary,
    string? Body,
    PageTemplate Template,
    string? CustomComponent,
    string? BannerImageUrl,
    string? BannerCaption,
    bool ShowSidebarNav,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? OgImageUrl,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<BreadcrumbDto> Breadcrumbs,
    /// <summary>Sibling pages under the same parent - rendered as the left sidebar navigation.</summary>
    IReadOnlyList<PageSummaryDto> SiblingPages,
    IReadOnlyList<PageBlockDto> Blocks);

public record PageBlockDto(
    int Id,
    BlockType Type,
    int SortOrder,
    string? Eyebrow,
    string? Heading,
    string? SubHeading,
    string? Body,
    string? ImageUrl,
    string? VideoUrl,
    string? PrimaryLinkText,
    string? PrimaryLinkUrl,
    string? SecondaryLinkText,
    string? SecondaryLinkUrl,
    string? SettingsJson);

// ------------------------------------------------------------------ home page ----

public record BannerDto(
    int Id,
    string? Eyebrow,
    string Title,
    string? HighlightedTitle,
    string? Subtitle,
    string ImageUrl,
    string? MobileImageUrl,
    string? AltText,
    string? PrimaryButtonText,
    string? PrimaryButtonUrl,
    string? SecondaryButtonText,
    string? SecondaryButtonUrl);

public record StatisticDto(int Id, string Label, long Value, string? Prefix, string? Suffix, string? Icon, string? LinkUrl);

public record SchemeComponentDto(int Id, string Title, string? ShortDescription, string? Description, string? Icon, string? LinkUrl);

public record SchemeLevelDto(
    int Id,
    LeanLevel Level,
    string Name,
    string? BadgeLabel,
    string? Tagline,
    string? Description,
    IReadOnlyList<string> Deliverables,
    string? FeeStructure,
    string? Duration,
    string? IconUrl,
    string? CertificateImageUrl,
    string? AccentColor);

public record LoginPortalDto(
    int Id,
    string Title,
    string? Audience,
    string? Description,
    string? Icon,
    string? LoginUrl,
    string? LoginText,
    string? RegisterUrl,
    string? RegisterText,
    string? AccentColor,
    bool OpenInNewTab);

public record TestimonialDto(
    int Id,
    string UnitName,
    string? PersonName,
    string? Designation,
    string? Location,
    string? Sector,
    string Quote,
    string? PhotoUrl,
    string? VideoUrl,
    LeanLevel? AchievedLevel,
    string? ImpactHighlight);

public record IncentiveDto(
    int Id,
    IncentiveCategory Category,
    string? Level,
    string? State,
    string Title,
    string? Description,
    string? IssuerName,
    string? IssuerLogoUrl,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Document1Url,
    string? Document1Label,
    string? Document2Url,
    string? Document2Label,
    string? VideoUrl,
    string? AvailUrl,
    string? AvailLabel);

/// <summary>
/// A category's incentives together with the filter values that actually occur in
/// them, so the page never offers a level or a state that would return nothing.
/// </summary>
public record IncentiveListDto(
    IReadOnlyList<IncentiveDto> Incentives,
    IReadOnlyList<string> Levels,
    IReadOnlyList<string> States,
    int TotalCount);

public record BenefitDto(
    int Id,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string? Icon,
    string? LinkUrl,
    string? LinkText,
    bool OpenInNewTab);

public record PartnerDto(
    int Id,
    PartnerType Type,
    string Name,
    string? ShortName,
    string? Description,
    string? EnquiryFormUrl,
    string? LogoUrl,
    string? WebsiteUrl,
    string? ContactUrl,
    string? Email,
    string? Phone,
    string? City,
    string? State);

/// <summary>Everything the Angular home page needs, in a single round trip.</summary>
public record HomeDto(
    PageDto Page,
    IReadOnlyList<BannerDto> Banners,
    IReadOnlyList<StatisticDto> Statistics,
    IReadOnlyList<SchemeComponentDto> SchemeComponents,
    IReadOnlyList<SchemeLevelDto> SchemeLevels,
    IReadOnlyList<LoginPortalDto> LoginPortals,
    IReadOnlyList<DocumentDto> FeaturedDocuments,
    IReadOnlyList<PostSummaryDto> LatestPosts,
    IReadOnlyList<PostSummaryDto> TickerPosts,
    IReadOnlyList<TestimonialDto> Testimonials,
    IReadOnlyList<PartnerDto> UsefulLinks,
    IReadOnlyList<GalleryImageDto> GalleryPhotos,
    IReadOnlyList<GalleryImageDto> GalleryVideos,
    IReadOnlyList<PartnerDto> Partners,
    IReadOnlyList<BenefitDto> Benefits);

// --------------------------------------------------------------------- posts ----

public record PostSummaryDto(
    int Id,
    PostType Type,
    string Slug,
    string Title,
    string? Excerpt,
    string? CoverImageUrl,
    string? AttachmentUrl,
    string? AttachmentLabel,
    bool IsFeatured,
    DateTimeOffset? PublishedAt);

public record PostDto(
    int Id,
    PostType Type,
    string Slug,
    string Title,
    string? Excerpt,
    string? Body,
    string? CoverImageUrl,
    string? Author,
    string? AttachmentUrl,
    string? AttachmentLabel,
    string? MetaDescription,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<PostSummaryDto> Related);

// ----------------------------------------------------------------- documents ----

public record DocumentDto(
    int Id,
    string Title,
    string? Description,
    DocumentCategory Category,
    string CategoryName,
    string FileUrl,
    string FileType,
    long FileSizeBytes,
    string FileSizeDisplay,
    string? Language,
    string? Version,
    DateTimeOffset? DocumentDate,
    int DownloadCount);

// ---------------------------------------------------------------------- faqs ----

public record FaqDto(int Id, string Question, string Answer, string? Category, bool IsFeatured);

public record FaqGroupDto(string Category, IReadOnlyList<FaqDto> Items);

// ------------------------------------------------------------------- gallery ----

public record GalleryImageDto(
    int Id, string ImageUrl, string? ThumbnailUrl, string? Caption, string? AltText,
    string? VideoUrl, int SortOrder, bool IsActive, string? AlbumTitle, string? AlbumSlug);

public record GalleryAlbumSummaryDto(
    int Id, string Slug, string Title, string? Description, string? CoverImageUrl,
    string? Location, DateTimeOffset? EventDate, int ImageCount, PublishStatus Status,
    int SortOrder);

public record GalleryAlbumDto(
    int Id, string Slug, string Title, string? Description, string? CoverImageUrl,
    string? Location, DateTimeOffset? EventDate, IReadOnlyList<GalleryImageDto> Images);

// ---------------------------------------------------------------- programmes ----

public record ProgrammeDto(
    int Id,
    string ProgrammeCode,
    string Title,
    string? Description,
    string ProgrammeType,
    string? Agency,
    string? State,
    string? District,
    string? Venue,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    int RegisteredCount,
    int? Capacity,
    string? RegistrationUrl,
    ProgrammeStatus ProgrammeStatus);

public record ProgrammeFiltersDto(
    IReadOnlyList<string> States,
    IReadOnlyList<string> Districts,
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Agencies);

// -------------------------------------------------------------------- forms ----

/// <summary>What the assistant found, and where each passage came from.</summary>
public record AssistantReplyDto(
    string Answer,
    IReadOnlyList<AssistantMatchDto> Sources,
    IReadOnlyList<string> Suggestions,
    // True when the answer came from an outside service rather than from this site,
    // so the panel does not claim to be quoting the portal when it is not.
    bool External = false);

/// <summary>One passage the assistant is offering, with the page it is published on.</summary>
public record AssistantMatchDto(string Title, string Snippet, string Url, string Kind, int Score);

public class ContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Organisation { get; set; }
    public string? UdyamNumber { get; set; }
    public string? State { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }
    /// <summary>Implementing agency the enquiry is for, where the sender picked one.</summary>
    public string? Agency { get; set; }
    /// <summary>The grievance matrix's four levels, for an agency that uses one.</summary>
    public string? UserType { get; set; }
    public string? IssueType { get; set; }
    public string? IssueCategory { get; set; }
    public string? IssueSubCategory { get; set; }
    /// <summary>Identifies the challenge the answer below belongs to.</summary>
    public string? CaptchaId { get; set; }
    public string? CaptchaAnswer { get; set; }
    /// <summary>Honeypot field - must stay empty. Bots fill it in.</summary>
    public string? Website { get; set; }
}

public class SubscribeRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Website { get; set; }
}

// ------------------------------------------------------------------ site meta ----

public record SiteSettingsDto(IReadOnlyDictionary<string, string?> Settings);

public record SitemapNodeDto(string Title, string? Url, IReadOnlyList<SitemapNodeDto> Children);

/// <summary>Running visitor total. <c>Enabled</c> is false when the counter is switched off in the CMS.</summary>
public record VisitorCountDto(long Total, bool Enabled);
