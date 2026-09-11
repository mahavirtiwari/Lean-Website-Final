using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>A downloadable file surfaced in the "Documents &amp; Notices" section.</summary>
public class DocumentItem : AuditableEntity, IPublishable
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;

    public string FileUrl { get; set; } = string.Empty;
    /// <summary>Upper-case extension shown on the download chip, e.g. PDF, DOCX.</summary>
    public string FileType { get; set; } = "PDF";
    public long FileSizeBytes { get; set; }
    public string? Language { get; set; } = "English";
    public string? Version { get; set; }
    public DateTimeOffset? DocumentDate { get; set; }

    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public int DownloadCount { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Published;
    public DateTimeOffset? PublishedAt { get; set; }
}
