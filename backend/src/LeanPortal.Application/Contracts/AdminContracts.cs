using System.ComponentModel.DataAnnotations;
using LeanPortal.Application.Common;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Application.Contracts;

// ------------------------------------------------------------------- auth ----

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>The challenge on the sign-in form, and the answer to it.</summary>
    [MaxLength(64)] public string? CaptchaId { get; set; }
    [MaxLength(16)] public string? CaptchaAnswer { get; set; }

    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(12, ErrorMessage = "The new password must be at least 12 characters long.")]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    CurrentUserDto User);

public record CurrentUserDto(
    string Id,
    string Email,
    string FullName,
    string? Designation,
    string? Department,
    string? AvatarUrl,
    bool MustChangePassword,
    IReadOnlyList<string> Roles);

// ------------------------------------------------------------------ users ----

public record AdminUserDto(
    string Id,
    string Email,
    string FullName,
    string? Designation,
    string? Department,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

public class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    [MaxLength(200)] public string? Designation { get; set; }
    [MaxLength(200)] public string? Department { get; set; }

    [Required, MinLength(12), MaxLength(128)] public string Password { get; set; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Assign at least one role.")]
    public List<string> Roles { get; set; } = [];
}

public class UpdateUserRequest
{
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    [MaxLength(200)] public string? Designation { get; set; }
    [MaxLength(200)] public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = [];
}

public class ResetPasswordRequest
{
    [Required, MinLength(12), MaxLength(128)] public string NewPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
}

// ------------------------------------------------------------------ pages ----

public record AdminPageListItemDto(
    int Id, string Slug, string Title, string? ParentTitle, PageTemplate Template,
    PublishStatus Status, int SortOrder, DateTimeOffset? PublishedAt, DateTimeOffset? UpdatedAt, string? UpdatedBy);

public record AdminPageDto(
    int Id, string Slug, string Title, string? ShortTitle, string? Summary, string? Body,
    PageTemplate Template, string? CustomComponent, int? ParentId, int SortOrder,
    string? BannerImageUrl, string? BannerCaption, bool ShowInMainMenu, bool ShowSidebarNav,
    string? MetaTitle, string? MetaDescription, string? MetaKeywords, string? OgImageUrl,
    PublishStatus Status, DateTimeOffset? PublishedAt, int ViewCount,
    DateTimeOffset CreatedAt, string? CreatedBy, DateTimeOffset? UpdatedAt, string? UpdatedBy,
    IReadOnlyList<AdminPageBlockDto> Blocks);

public class SavePageRequest
{
    [Required, MaxLength(200), RegularExpression(@"^[a-z0-9]+(?:[-/][a-z0-9]+)*$",
        ErrorMessage = "Slug may contain lower-case letters, digits, hyphens and forward slashes only.")]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    [MaxLength(150)] public string? ShortTitle { get; set; }
    [MaxLength(1000)] public string? Summary { get; set; }
    public string? Body { get; set; }

    public PageTemplate Template { get; set; } = PageTemplate.SidebarLeft;
    [MaxLength(100)] public string? CustomComponent { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }

    [MaxLength(500)] public string? BannerImageUrl { get; set; }
    [MaxLength(300)] public string? BannerCaption { get; set; }
    public bool ShowInMainMenu { get; set; }
    public bool ShowSidebarNav { get; set; } = true;

    [MaxLength(300)] public string? MetaTitle { get; set; }
    [MaxLength(500)] public string? MetaDescription { get; set; }
    [MaxLength(500)] public string? MetaKeywords { get; set; }
    [MaxLength(500)] public string? OgImageUrl { get; set; }
}

public record AdminPageBlockDto(
    int Id, BlockType Type, int SortOrder, bool IsVisible,
    string? Eyebrow, string? Heading, string? SubHeading, string? Body,
    string? ImageUrl, string? VideoUrl,
    string? PrimaryLinkText, string? PrimaryLinkUrl,
    string? SecondaryLinkText, string? SecondaryLinkUrl, string? SettingsJson);

public class SavePageBlockRequest
{
    public BlockType Type { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    [MaxLength(200)] public string? Eyebrow { get; set; }
    [MaxLength(300)] public string? Heading { get; set; }
    [MaxLength(500)] public string? SubHeading { get; set; }
    public string? Body { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    [MaxLength(500)] public string? VideoUrl { get; set; }
    [MaxLength(120)] public string? PrimaryLinkText { get; set; }
    [MaxLength(500)] public string? PrimaryLinkUrl { get; set; }
    [MaxLength(120)] public string? SecondaryLinkText { get; set; }
    [MaxLength(500)] public string? SecondaryLinkUrl { get; set; }
    public string? SettingsJson { get; set; }
}

/// <summary>Moves a publishable entity through the editorial workflow.</summary>
public class ChangeStatusRequest
{
    public PublishStatus Status { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

/// <summary>Persists a drag-and-drop re-order in one request.</summary>
public class ReorderRequest
{
    [Required, MinLength(1)] public List<ReorderItem> Items { get; set; } = [];
}

public record ReorderItem(int Id, int SortOrder);

// ------------------------------------------------------------------- posts ----

public record AdminPostListItemDto(
    int Id, PostType Type, string Slug, string Title, bool IsFeatured, bool ShowInTicker,
    PublishStatus Status, DateTimeOffset? PublishedAt, DateTimeOffset? UpdatedAt, string? UpdatedBy);

public class SavePostRequest
{
    public PostType Type { get; set; } = PostType.News;

    [Required, MaxLength(250), RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug may contain lower-case letters, digits and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(400)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Excerpt { get; set; }
    public string? Body { get; set; }
    [MaxLength(500)] public string? CoverImageUrl { get; set; }
    [MaxLength(200)] public string? Author { get; set; }
    [MaxLength(500)] public string? AttachmentUrl { get; set; }
    [MaxLength(200)] public string? AttachmentLabel { get; set; }
    public bool IsFeatured { get; set; }
    public bool ShowInTicker { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    [MaxLength(500)] public string? MetaDescription { get; set; }
}

// --------------------------------------------------------------- documents ----

public class SaveDocumentRequest
{
    [Required, MaxLength(400)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;
    [Required, MaxLength(500)] public string FileUrl { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string FileType { get; set; } = "PDF";
    public long FileSizeBytes { get; set; }
    [MaxLength(50)] public string? Language { get; set; } = "English";
    [MaxLength(50)] public string? Version { get; set; }
    public DateTimeOffset? DocumentDate { get; set; }
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    /// <summary>Published, Draft or Archived. Draft and Archived hide the record from
    /// the public site without deleting it.</summary>
    public PublishStatus Status { get; set; } = PublishStatus.Published;
}

// ----------------------------------------------------------------- banners ----

public class SaveBannerRequest
{
    [MaxLength(200)] public string? Eyebrow { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    [MaxLength(300)] public string? HighlightedTitle { get; set; }
    [MaxLength(800)] public string? Subtitle { get; set; }
    [Required, MaxLength(500)] public string ImageUrl { get; set; } = string.Empty;
    [MaxLength(500)] public string? MobileImageUrl { get; set; }

    [Required(ErrorMessage = "Alternative text is required for accessibility compliance."), MaxLength(300)]
    public string? AltText { get; set; }

    [MaxLength(120)] public string? PrimaryButtonText { get; set; }
    [MaxLength(500)] public string? PrimaryButtonUrl { get; set; }
    [MaxLength(120)] public string? SecondaryButtonText { get; set; }
    [MaxLength(500)] public string? SecondaryButtonUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}

// -------------------------------------------------------------------- menu ----

public record AdminMenuItemDto(
    int Id, MenuLocation Location, string Label, string? Url, int? PageId, string? PageSlug,
    int? ParentId, int SortOrder, bool OpenInNewTab, bool IsActive, string? Icon, bool IsHighlighted,
    IReadOnlyList<AdminMenuItemDto> Children);

public class SaveMenuItemRequest
{
    public MenuLocation Location { get; set; } = MenuLocation.Main;
    [Required, MaxLength(150)] public string Label { get; set; } = string.Empty;
    [MaxLength(500)] public string? Url { get; set; }
    public int? PageId { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(80)] public string? Icon { get; set; }
    public bool IsHighlighted { get; set; }
}

// --------------------------------------------------- scheme reference data ----

public class SaveSchemeLevelRequest
{
    public LeanLevel Level { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(60)] public string? BadgeLabel { get; set; }
    [MaxLength(300)] public string? Tagline { get; set; }
    public string? Description { get; set; }
    public string? Deliverables { get; set; }
    [MaxLength(500)] public string? FeeStructure { get; set; }
    [MaxLength(100)] public string? Duration { get; set; }
    [MaxLength(500)] public string? IconUrl { get; set; }
    [MaxLength(500)] public string? CertificateImageUrl { get; set; }
    [MaxLength(20)] public string? AccentColor { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveSchemeComponentRequest
{
    [Required, MaxLength(250)] public string Title { get; set; } = string.Empty;
    [MaxLength(500)] public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    [MaxLength(80)] public string? Icon { get; set; }
    [MaxLength(500)] public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveStatisticRequest
{
    [Required, MaxLength(200)] public string Label { get; set; } = string.Empty;
    public long Value { get; set; }
    [MaxLength(20)] public string? Prefix { get; set; }
    [MaxLength(20)] public string? Suffix { get; set; }
    [MaxLength(80)] public string? Icon { get; set; }
    [MaxLength(500)] public string? LinkUrl { get; set; }
    [MaxLength(100)] public string? SourceQueryKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveLoginPortalRequest
{
    [Required, MaxLength(250)] public string Title { get; set; } = string.Empty;
    [MaxLength(150)] public string? Audience { get; set; }
    [MaxLength(600)] public string? Description { get; set; }
    [MaxLength(80)] public string? Icon { get; set; }
    [MaxLength(500)] public string? LoginUrl { get; set; }
    [MaxLength(80)] public string? LoginText { get; set; }
    [MaxLength(500)] public string? RegisterUrl { get; set; }
    [MaxLength(80)] public string? RegisterText { get; set; }
    [MaxLength(20)] public string? AccentColor { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool OpenInNewTab { get; set; } = true;
}

public class SaveIncentiveRequest
{
    public IncentiveCategory Category { get; set; }
    [MaxLength(80)] public string? Level { get; set; }
    [MaxLength(120)] public string? State { get; set; }
    [Required, MaxLength(400)] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [MaxLength(300)] public string? IssuerName { get; set; }
    [MaxLength(500)] public string? IssuerLogoUrl { get; set; }
    [MaxLength(300)] public string? ContactName { get; set; }
    [EmailAddress, MaxLength(256)] public string? ContactEmail { get; set; }
    [MaxLength(120)] public string? ContactPhone { get; set; }
    [MaxLength(500)] public string? Document1Url { get; set; }
    [MaxLength(120)] public string? Document1Label { get; set; }
    [MaxLength(500)] public string? Document2Url { get; set; }
    [MaxLength(120)] public string? Document2Label { get; set; }
    [MaxLength(500)] public string? VideoUrl { get; set; }
    [MaxLength(500)] public string? AvailUrl { get; set; }
    [MaxLength(60)] public string? AvailLabel { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveBenefitRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(300)] public string? Subtitle { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    [MaxLength(80)] public string? Icon { get; set; }
    [MaxLength(500)] public string? LinkUrl { get; set; }
    [MaxLength(60)] public string? LinkText { get; set; }
    public bool OpenInNewTab { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SavePartnerRequest
{
    public PartnerType Type { get; set; }
    [Required, MaxLength(400)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? ShortName { get; set; }
    public string? Description { get; set; }
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(500)] public string? WebsiteUrl { get; set; }
    [MaxLength(500)] public string? ContactUrl { get; set; }
    [EmailAddress, MaxLength(256)] public string? Email { get; set; }
    [EmailAddress, MaxLength(256)] public string? EnquiryEmail { get; set; }
    [MaxLength(500)] public string? EnquiryFormUrl { get; set; }
    [MaxLength(60)] public string? Phone { get; set; }
    [MaxLength(600)] public string? Address { get; set; }
    [MaxLength(120)] public string? State { get; set; }
    [MaxLength(120)] public string? City { get; set; }
    [MaxLength(100)] public string? RegistrationNumber { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
}

// -------------------------------------------------------------------- faqs ----

public class SaveFaqRequest
{
    [Required, MaxLength(600)] public string Question { get; set; } = string.Empty;
    [Required] public string Answer { get; set; } = string.Empty;
    [MaxLength(120)] public string? Category { get; set; } = "General";
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    /// <summary>Published, Draft or Archived. Draft and Archived hide the record from
    /// the public site without deleting it.</summary>
    public PublishStatus Status { get; set; } = PublishStatus.Published;
}

// ------------------------------------------------------------- testimonials ----

public class SaveTestimonialRequest
{
    [Required, MaxLength(300)] public string UnitName { get; set; } = string.Empty;
    [MaxLength(200)] public string? PersonName { get; set; }
    [MaxLength(200)] public string? Designation { get; set; }
    [MaxLength(200)] public string? Location { get; set; }
    [MaxLength(200)] public string? Sector { get; set; }
    [Required, MaxLength(2000)] public string Quote { get; set; } = string.Empty;
    [MaxLength(500)] public string? PhotoUrl { get; set; }
    [MaxLength(500)] public string? VideoUrl { get; set; }
    public LeanLevel? AchievedLevel { get; set; }
    [MaxLength(300)] public string? ImpactHighlight { get; set; }
    public int SortOrder { get; set; }
    /// <summary>Published, Draft or Archived. Draft and Archived hide the record from
    /// the public site without deleting it.</summary>
    public PublishStatus Status { get; set; } = PublishStatus.Published;
}

// ----------------------------------------------------------------- gallery ----

public class SaveGalleryAlbumRequest
{
    [Required, MaxLength(200), RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(500)] public string? CoverImageUrl { get; set; }
    [MaxLength(200)] public string? Location { get; set; }
    public DateTimeOffset? EventDate { get; set; }
    public int SortOrder { get; set; }
}

public class SaveGalleryImageRequest
{
    [Required, MaxLength(500)] public string ImageUrl { get; set; } = string.Empty;
    [MaxLength(500)] public string? ThumbnailUrl { get; set; }

    /// <summary>Makes the item a video; the image above becomes its poster.</summary>
    [MaxLength(500)] public string? VideoUrl { get; set; }

    [MaxLength(400)] public string? Caption { get; set; }

    [Required(ErrorMessage = "Alternative text is required for accessibility compliance."), MaxLength(300)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Defaults to on, so an older client that omits it still adds a visible item.</summary>
    public bool IsActive { get; set; } = true;
}

// -------------------------------------------------------------- programmes ----

public class SaveProgrammeRequest
{
    [Required, MaxLength(60)] public string ProgrammeCode { get; set; } = string.Empty;
    [Required, MaxLength(400)] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required, MaxLength(100)] public string ProgrammeType { get; set; } = "Awareness Programme";
    [MaxLength(100)] public string? Agency { get; set; }
    [MaxLength(120)] public string? State { get; set; }
    [MaxLength(120)] public string? District { get; set; }
    [MaxLength(500)] public string? Venue { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int RegisteredCount { get; set; }
    public int? Capacity { get; set; }
    [MaxLength(500)] public string? RegistrationUrl { get; set; }
    public ProgrammeStatus ProgrammeStatus { get; set; } = ProgrammeStatus.Upcoming;
    /// <summary>Published, Draft or Archived. Draft and Archived hide the record from
    /// the public site without deleting it.</summary>
    public PublishStatus Status { get; set; } = PublishStatus.Published;
}

// --------------------------------------------------------------- enquiries ----

public record AdminContactMessageDto(
    int Id, string Name, string Email, string? Phone, string? Organisation, string? UdyamNumber,
    string? State, string Subject, string Message, string? Category, string? Agency,
    IReadOnlyList<EnquiryAttachmentDto> Attachments,
    ContactMessageStatus Status, string? AssignedTo, string? InternalNotes,
    DateTimeOffset CreatedAt, DateTimeOffset? RespondedAt);

/// <summary>A file sent with an enquiry.</summary>
public record EnquiryAttachmentDto(string Name, string Url, long SizeBytes);

public class UpdateContactMessageRequest
{
    public ContactMessageStatus Status { get; set; }
    [MaxLength(256)] public string? AssignedTo { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
}

// ---------------------------------------------------------------- settings ----

public record AdminSettingDto(
    int Id, string Key, string? Value, string? DisplayName, string? Description,
    string Group, string? Section, string DataType, int SortOrder, bool IsPublic);

public class SaveSettingsRequest
{
    [Required, MinLength(1)] public Dictionary<string, string?> Values { get; set; } = [];
}

// ------------------------------------------------------------------- media ----

public record MediaAssetDto(
    int Id, string FileName, string Url, string? ThumbnailUrl, string ContentType,
    long SizeBytes, string SizeDisplay, int? Width, int? Height,
    string? AltText, string? Caption, string? Folder, DateTimeOffset CreatedAt);

public class UpdateMediaAssetRequest
{
    [MaxLength(300)] public string? AltText { get; set; }
    [MaxLength(500)] public string? Caption { get; set; }
    [MaxLength(120)] public string? Folder { get; set; }
}

// --------------------------------------------------------------- dashboard ----

public record DashboardDto(
    int PublishedPages,
    int DraftPages,
    int PublishedPosts,
    int Documents,
    int NewEnquiries,
    int TotalEnquiries,
    int GalleryImages,
    int UpcomingProgrammes,
    int ActiveUsers,
    IReadOnlyList<AdminPageListItemDto> RecentlyUpdatedPages,
    IReadOnlyList<AdminContactMessageDto> RecentEnquiries,
    IReadOnlyList<AuditLogDto> RecentActivity);

public record AuditLogDto(
    int Id, DateTimeOffset Timestamp, string? UserName, string Action,
    string EntityName, string? EntityId, string? Changes, string? IpAddress);

// =============================================================================
//  Admin projections for the uniform resources.
//
//  The public DTOs deliberately omit editorial fields such as SortOrder,
//  IsActive and scheduling. The admin screens must round-trip the full editable
//  surface, otherwise saving a record would blank the fields the form never saw.
// =============================================================================

public record AdminBannerDto(
    int Id, string? Eyebrow, string Title, string? HighlightedTitle, string? Subtitle,
    string ImageUrl, string? MobileImageUrl, string? AltText,
    string? PrimaryButtonText, string? PrimaryButtonUrl,
    string? SecondaryButtonText, string? SecondaryButtonUrl,
    int SortOrder, bool IsActive, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);

public record AdminFaqDto(
    int Id, string Question, string Answer, string? Category, int SortOrder, bool IsFeatured,
    PublishStatus Status);

public record AdminDocumentDto(
    int Id, string Title, string? Description, DocumentCategory Category, string CategoryName,
    string FileUrl, string FileType, long FileSizeBytes, string FileSizeDisplay,
    string? Language, string? Version, DateTimeOffset? DocumentDate,
    int SortOrder, bool IsFeatured, int DownloadCount, PublishStatus Status);

public record AdminStatisticDto(
    int Id, string Label, long Value, string? Prefix, string? Suffix, string? Icon,
    string? LinkUrl, string? SourceQueryKey, int SortOrder, bool IsActive,
    DateTimeOffset? LastSyncedAt);

public record AdminLoginPortalDto(
    int Id, string Title, string? Audience, string? Description, string? Icon,
    string? LoginUrl, string? LoginText, string? RegisterUrl, string? RegisterText,
    string? AccentColor, int SortOrder, bool IsActive, bool OpenInNewTab);

public record AdminSchemeLevelDto(
    int Id, LeanLevel Level, string Name, string? BadgeLabel, string? Tagline, string? Description,
    string? Deliverables, string? FeeStructure, string? Duration, string? IconUrl,
    string? CertificateImageUrl, string? AccentColor, int SortOrder, bool IsActive);

public record AdminSchemeComponentDto(
    int Id, string Title, string? ShortDescription, string? Description, string? Icon,
    string? LinkUrl, int SortOrder, bool IsActive);

public record AdminTestimonialDto(
    int Id, string UnitName, string? PersonName, string? Designation, string? Location, string? Sector,
    string Quote, string? PhotoUrl, string? VideoUrl, LeanLevel? AchievedLevel,
    string? ImpactHighlight, int SortOrder, PublishStatus Status);

public record AdminIncentiveDto(
    int Id, IncentiveCategory Category, string? Level, string? State, string Title,
    string? Description, string? IssuerName, string? IssuerLogoUrl,
    string? ContactName, string? ContactEmail, string? ContactPhone,
    string? Document1Url, string? Document1Label, string? Document2Url, string? Document2Label,
    string? VideoUrl, string? AvailUrl, string? AvailLabel, int SortOrder, bool IsActive);

public class MailTestRequest
{
    [Required, EmailAddress, MaxLength(256)] public string To { get; set; } = string.Empty;

    /// <summary>Send through this agency's server, as its enquiries would go. Empty: the portal's.</summary>
    public int? PartnerId { get; set; }
}

/// <summary>A mail server as the console edits it. The password is never sent back; only whether one is set.</summary>
public record MailServerDto(
    string? Host, int Port, string Encryption, string? Username, bool HasPassword,
    string? FromAddress, string? FromName);

public record MailServerSaveDto(
    string? Host, int Port, string? Encryption, string? Username,
    /// <summary>Empty keeps the stored one.</summary>
    string? Password,
    string? FromAddress, string? FromName);

/// <summary>One agency's enquiry mail: the inbox it reads, and optionally a server of its own.</summary>
public record AgencyMailDto(
    int PartnerId, string Name, string? ShortName,
    string? EnquiryEmail, string? PublishedEmail,
    bool UseOwnServer, MailServerDto Server,
    /// <summary>True when its enquiries are raised in Zoho Desk, so mail is only the fallback.</summary>
    bool HelpdeskActive);

public record MailConfigDto(MailServerDto Portal, string? CopyTo, IReadOnlyList<AgencyMailDto> Agencies);

public record AgencyMailSaveDto(int PartnerId, string? EnquiryEmail, bool UseOwnServer, MailServerSaveDto Server);

public record MailConfigSaveDto(MailServerSaveDto Portal, string? CopyTo, IReadOnlyList<AgencyMailSaveDto> Agencies);

public record AdminBenefitDto(
    int Id, string Title, string? Subtitle, string? ImageUrl, string? Icon,
    string? LinkUrl, string? LinkText, bool OpenInNewTab, int SortOrder, bool IsActive);

public record AdminPartnerDto(
    int Id, PartnerType Type, string Name, string? ShortName, string? Description, string? LogoUrl,
    string? WebsiteUrl, string? ContactUrl, string? Email, string? EnquiryEmail,
    string? EnquiryFormUrl, string? Phone, string? Address,
    string? State, string? City, string? RegistrationNumber,
    int SortOrder, bool IsActive, bool IsFeatured);

public record AdminProgrammeDto(
    int Id, string ProgrammeCode, string Title, string? Description, string ProgrammeType,
    string? Agency, string? State, string? District, string? Venue,
    DateTimeOffset StartDate, DateTimeOffset? EndDate, int RegisteredCount, int? Capacity,
    string? RegistrationUrl, ProgrammeStatus ProgrammeStatus, PublishStatus Status);
