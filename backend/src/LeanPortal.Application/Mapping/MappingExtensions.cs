using LeanPortal.Application.Contracts;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Application.Mapping;

/// <summary>Entity to DTO projections. Kept explicit so the API shape never drifts with the schema.</summary>
public static class MappingExtensions
{
    // ------------------------------------------------------------- helpers ----

    public static string ToFileSizeDisplay(this long bytes) => bytes switch
    {
        < 0 => "-",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024d:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024d * 1024):0.#} MB",
        _ => $"{bytes / (1024d * 1024 * 1024):0.##} GB"
    };

    public static string ToDisplayName(this DocumentCategory category) => category switch
    {
        DocumentCategory.SchemeGuideline => "Scheme Guidelines",
        DocumentCategory.Brochure => "Brochures",
        DocumentCategory.Circular => "Circulars",
        DocumentCategory.Format => "Formats & Templates",
        DocumentCategory.Presentation => "Presentations",
        DocumentCategory.Report => "Reports",
        DocumentCategory.Policy => "Policy Documents",
        _ => "Other Documents"
    };

    private static IReadOnlyList<string> SplitLines(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // --------------------------------------------------------------- pages ----

    public static PageSummaryDto ToSummaryDto(this Page p) =>
        new(p.Id, p.Slug, p.Title, p.ShortTitle, p.Summary, p.SortOrder);

    public static PageBlockDto ToDto(this PageBlock b) =>
        new(b.Id, b.Type, b.SortOrder, b.Eyebrow, b.Heading, b.SubHeading, b.Body, b.ImageUrl, b.VideoUrl,
            b.PrimaryLinkText, b.PrimaryLinkUrl, b.SecondaryLinkText, b.SecondaryLinkUrl, b.SettingsJson);

    public static PageDto ToDto(
        this Page p,
        IReadOnlyList<BreadcrumbDto>? breadcrumbs = null,
        IReadOnlyList<PageSummaryDto>? siblings = null) =>
        new(p.Id, p.Slug, p.Title, p.ShortTitle, p.Summary, p.Body, p.Template, p.CustomComponent,
            p.BannerImageUrl, p.BannerCaption, p.ShowSidebarNav,
            p.MetaTitle, p.MetaDescription, p.MetaKeywords, p.OgImageUrl,
            p.PublishedAt, p.UpdatedAt,
            breadcrumbs ?? [],
            siblings ?? [],
            p.Blocks.Where(b => b.IsVisible).OrderBy(b => b.SortOrder).Select(ToDto).ToList());

    public static AdminPageBlockDto ToAdminDto(this PageBlock b) =>
        new(b.Id, b.Type, b.SortOrder, b.IsVisible, b.Eyebrow, b.Heading, b.SubHeading, b.Body,
            b.ImageUrl, b.VideoUrl, b.PrimaryLinkText, b.PrimaryLinkUrl,
            b.SecondaryLinkText, b.SecondaryLinkUrl, b.SettingsJson);

    public static AdminPageDto ToAdminDto(this Page p) =>
        new(p.Id, p.Slug, p.Title, p.ShortTitle, p.Summary, p.Body, p.Template, p.CustomComponent,
            p.ParentId, p.SortOrder, p.BannerImageUrl, p.BannerCaption, p.ShowInMainMenu, p.ShowSidebarNav,
            p.MetaTitle, p.MetaDescription, p.MetaKeywords, p.OgImageUrl,
            p.Status, p.PublishedAt, p.ViewCount,
            p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy,
            p.Blocks.OrderBy(b => b.SortOrder).Select(ToAdminDto).ToList());

    public static AdminPageListItemDto ToListItemDto(this Page p) =>
        new(p.Id, p.Slug, p.Title, p.Parent?.Title, p.Template, p.Status, p.SortOrder,
            p.PublishedAt, p.UpdatedAt, p.UpdatedBy);

    // ---------------------------------------------------------------- menu ----

    public static MenuItemDto ToDto(this MenuItem m) =>
        new(m.Id, m.Label, m.Url ?? (m.Page is null ? null : "/" + m.Page.Slug), m.Page?.Slug, m.Icon,
            m.OpenInNewTab, m.IsHighlighted,
            m.Children.Where(c => c.IsActive).OrderBy(c => c.SortOrder).Select(ToDto).ToList());

    public static AdminMenuItemDto ToAdminDto(this MenuItem m) =>
        new(m.Id, m.Location, m.Label, m.Url, m.PageId, m.Page?.Slug, m.ParentId, m.SortOrder,
            m.OpenInNewTab, m.IsActive, m.Icon, m.IsHighlighted,
            m.Children.OrderBy(c => c.SortOrder).Select(ToAdminDto).ToList());

    // ------------------------------------------------------------ home page ----

    public static BannerDto ToDto(this Banner b) =>
        new(b.Id, b.Eyebrow, b.Title, b.HighlightedTitle, b.Subtitle, b.ImageUrl, b.MobileImageUrl, b.AltText,
            b.PrimaryButtonText, b.PrimaryButtonUrl, b.SecondaryButtonText, b.SecondaryButtonUrl);

    public static StatisticDto ToDto(this Statistic s) =>
        new(s.Id, s.Label, s.Value, s.Prefix, s.Suffix, s.Icon, s.LinkUrl);

    public static SchemeComponentDto ToDto(this SchemeComponent c) =>
        new(c.Id, c.Title, c.ShortDescription, c.Description, c.Icon, c.LinkUrl);

    public static SchemeLevelDto ToDto(this SchemeLevel l) =>
        new(l.Id, l.Level, l.Name, l.BadgeLabel, l.Tagline, l.Description, SplitLines(l.Deliverables),
            l.FeeStructure, l.Duration, l.IconUrl, l.CertificateImageUrl, l.AccentColor);

    public static LoginPortalDto ToDto(this LoginPortal p) =>
        new(p.Id, p.Title, p.Audience, p.Description, p.Icon, p.LoginUrl, p.LoginText,
            p.RegisterUrl, p.RegisterText, p.AccentColor, p.OpenInNewTab);

    public static TestimonialDto ToDto(this Testimonial t) =>
        new(t.Id, t.UnitName, t.PersonName, t.Designation, t.Location, t.Sector, t.Quote,
            t.PhotoUrl, t.VideoUrl, t.AchievedLevel, t.ImpactHighlight);

    public static IncentiveDto ToDto(this Incentive i) =>
        new(i.Id, i.Category, i.Level, i.State, i.Title, i.Description, i.IssuerName,
            i.IssuerLogoUrl, i.ContactName, i.ContactEmail, i.ContactPhone,
            i.Document1Url, i.Document1Label, i.Document2Url, i.Document2Label,
            i.VideoUrl, i.AvailUrl, i.AvailLabel);

    public static BenefitDto ToDto(this Benefit b) =>
        new(b.Id, b.Title, b.Subtitle, b.ImageUrl, b.Icon, b.LinkUrl, b.LinkText, b.OpenInNewTab);

    // EnquiryEmail is deliberately absent: it routes post, and putting it in a
    // public response would publish an inbox for anyone scraping the page.
    public static PartnerDto ToDto(this Partner p) =>
        new(p.Id, p.Type, p.Name, p.ShortName, p.Description, p.EnquiryFormUrl, p.LogoUrl,
            p.WebsiteUrl, p.ContactUrl, p.Email, p.Phone, p.City, p.State);

    // --------------------------------------------------------------- posts ----

    public static PostSummaryDto ToSummaryDto(this Post p) =>
        new(p.Id, p.Type, p.Slug, p.Title, p.Excerpt, p.CoverImageUrl, p.AttachmentUrl, p.AttachmentLabel,
            p.IsFeatured, p.PublishedAt);

    public static PostDto ToDto(this Post p, IReadOnlyList<PostSummaryDto>? related = null) =>
        new(p.Id, p.Type, p.Slug, p.Title, p.Excerpt, p.Body, p.CoverImageUrl, p.Author,
            p.AttachmentUrl, p.AttachmentLabel, p.MetaDescription, p.PublishedAt, p.UpdatedAt, related ?? []);

    public static AdminPostListItemDto ToListItemDto(this Post p) =>
        new(p.Id, p.Type, p.Slug, p.Title, p.IsFeatured, p.ShowInTicker, p.Status,
            p.PublishedAt, p.UpdatedAt, p.UpdatedBy);

    // ----------------------------------------------------------- documents ----

    public static DocumentDto ToDto(this DocumentItem d) =>
        new(d.Id, d.Title, d.Description, d.Category, d.Category.ToDisplayName(), d.FileUrl, d.FileType,
            d.FileSizeBytes, d.FileSizeBytes.ToFileSizeDisplay(), d.Language, d.Version, d.DocumentDate,
            d.DownloadCount);

    // ---------------------------------------------------------------- faqs ----

    public static FaqDto ToDto(this Faq f) => new(f.Id, f.Question, f.Answer, f.Category, f.IsFeatured);

    // ------------------------------------------------------------- gallery ----

    public static GalleryImageDto ToDto(this GalleryImage i) =>
        new(i.Id, i.ImageUrl, i.ThumbnailUrl, i.Caption, i.AltText, i.VideoUrl, i.SortOrder,
            i.IsActive, i.Album?.Title, i.Album?.Slug);

    public static GalleryAlbumSummaryDto ToSummaryDto(this GalleryAlbum a) =>
        new(a.Id, a.Slug, a.Title, a.Description, a.CoverImageUrl, a.Location, a.EventDate,
            a.Images.Count, a.Status, a.SortOrder);

    public static GalleryAlbumDto ToDto(this GalleryAlbum a) =>
        new(a.Id, a.Slug, a.Title, a.Description, a.CoverImageUrl, a.Location, a.EventDate,
            a.Images.Where(i => i.IsActive).OrderBy(i => i.SortOrder).Select(ToDto).ToList());

    // ---------------------------------------------------------- programmes ----

    public static ProgrammeDto ToDto(this AwarenessProgramme p) =>
        new(p.Id, p.ProgrammeCode, p.Title, p.Description, p.ProgrammeType, p.Agency, p.State, p.District,
            p.Venue, p.StartDate, p.EndDate, p.RegisteredCount, p.Capacity, p.RegistrationUrl, p.ProgrammeStatus);

    // ----------------------------------------------------------- enquiries ----

    public static AdminContactMessageDto ToAdminDto(this ContactMessage m) =>
        new(m.Id, m.Name, m.Email, m.Phone, m.Organisation, m.UdyamNumber, m.State, m.Subject, m.Message,
            m.Category, m.Agency, ReadAttachments(m.AttachmentsJson), m.Status, m.AssignedTo, m.InternalNotes,
            m.CreatedAt, m.RespondedAt);

    /// <summary>
    /// Attachments are stored as JSON on the message. Anything unreadable is treated
    /// as none: a console screen must still open for an enquiry whose list was
    /// written by an older version or damaged by hand.
    /// </summary>
    private static IReadOnlyList<EnquiryAttachmentDto> ReadAttachments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<EnquiryAttachmentDto>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    // ------------------------------------------------------------ settings ----

    public static AdminSettingDto ToAdminDto(this SiteSetting s) =>
        new(s.Id, s.Key, s.Value, s.DisplayName, s.Description, s.Group, s.Section, s.DataType,
            s.SortOrder, s.IsPublic);

    // --------------------------------------------------------------- media ----

    public static MediaAssetDto ToDto(this MediaAsset m) =>
        new(m.Id, m.FileName, m.Url, m.ThumbnailUrl, m.ContentType, m.SizeBytes,
            m.SizeBytes.ToFileSizeDisplay(), m.Width, m.Height, m.AltText, m.Caption, m.Folder, m.CreatedAt);

    public static AuditLogDto ToDto(this AuditLog a) =>
        new(a.Id, a.Timestamp, a.UserName, a.Action, a.EntityName, a.EntityId, a.Changes, a.IpAddress);
}

/// <summary>
/// Admin projections. These carry the full editable surface (sort order, active
/// flags, scheduling) that the public DTOs omit, so an admin form round-trips a
/// record without silently clearing fields it never displayed.
/// </summary>
public static class AdminMappingExtensions
{
    public static AdminBannerDto ToAdminDto(this Banner b) =>
        new(b.Id, b.Eyebrow, b.Title, b.HighlightedTitle, b.Subtitle, b.ImageUrl, b.MobileImageUrl,
            b.AltText, b.PrimaryButtonText, b.PrimaryButtonUrl, b.SecondaryButtonText, b.SecondaryButtonUrl,
            b.SortOrder, b.IsActive, b.StartsAt, b.EndsAt);

    public static AdminFaqDto ToAdminDto(this Faq f) =>
        new(f.Id, f.Question, f.Answer, f.Category, f.SortOrder, f.IsFeatured, f.Status);

    public static AdminDocumentDto ToAdminDto(this DocumentItem d) =>
        new(d.Id, d.Title, d.Description, d.Category, d.Category.ToDisplayName(), d.FileUrl, d.FileType,
            d.FileSizeBytes, d.FileSizeBytes.ToFileSizeDisplay(), d.Language, d.Version, d.DocumentDate,
            d.SortOrder, d.IsFeatured, d.DownloadCount, d.Status);

    public static AdminStatisticDto ToAdminDto(this Statistic s) =>
        new(s.Id, s.Label, s.Value, s.Prefix, s.Suffix, s.Icon, s.LinkUrl, s.SourceQueryKey,
            s.SortOrder, s.IsActive, s.LastSyncedAt);

    public static AdminLoginPortalDto ToAdminDto(this LoginPortal p) =>
        new(p.Id, p.Title, p.Audience, p.Description, p.Icon, p.LoginUrl, p.LoginText,
            p.RegisterUrl, p.RegisterText, p.AccentColor, p.SortOrder, p.IsActive, p.OpenInNewTab);

    public static AdminSchemeLevelDto ToAdminDto(this SchemeLevel l) =>
        new(l.Id, l.Level, l.Name, l.BadgeLabel, l.Tagline, l.Description, l.Deliverables,
            l.FeeStructure, l.Duration, l.IconUrl, l.CertificateImageUrl, l.AccentColor,
            l.SortOrder, l.IsActive);

    public static AdminSchemeComponentDto ToAdminDto(this SchemeComponent c) =>
        new(c.Id, c.Title, c.ShortDescription, c.Description, c.Icon, c.LinkUrl, c.SortOrder, c.IsActive);

    public static AdminTestimonialDto ToAdminDto(this Testimonial t) =>
        new(t.Id, t.UnitName, t.PersonName, t.Designation, t.Location, t.Sector, t.Quote,
            t.PhotoUrl, t.VideoUrl, t.AchievedLevel, t.ImpactHighlight, t.SortOrder, t.Status);

    public static AdminIncentiveDto ToAdminDto(this Incentive i) =>
        new(i.Id, i.Category, i.Level, i.State, i.Title, i.Description, i.IssuerName,
            i.IssuerLogoUrl, i.ContactName, i.ContactEmail, i.ContactPhone,
            i.Document1Url, i.Document1Label, i.Document2Url, i.Document2Label,
            i.VideoUrl, i.AvailUrl, i.AvailLabel, i.SortOrder, i.IsActive);

    public static AdminBenefitDto ToAdminDto(this Benefit b) =>
        new(b.Id, b.Title, b.Subtitle, b.ImageUrl, b.Icon, b.LinkUrl, b.LinkText,
            b.OpenInNewTab, b.SortOrder, b.IsActive);

    public static AdminPartnerDto ToAdminDto(this Partner p) =>
        new(p.Id, p.Type, p.Name, p.ShortName, p.Description, p.LogoUrl, p.WebsiteUrl, p.ContactUrl,
            p.Email, p.EnquiryEmail, p.EnquiryFormUrl, p.Phone, p.Address, p.State, p.City,
            p.RegistrationNumber,
            p.SortOrder, p.IsActive, p.IsFeatured);

    public static AdminProgrammeDto ToAdminDto(this AwarenessProgramme p) =>
        new(p.Id, p.ProgrammeCode, p.Title, p.Description, p.ProgrammeType, p.Agency, p.State,
            p.District, p.Venue, p.StartDate, p.EndDate, p.RegisteredCount, p.Capacity,
            p.RegistrationUrl, p.ProgrammeStatus, p.Status);
}
