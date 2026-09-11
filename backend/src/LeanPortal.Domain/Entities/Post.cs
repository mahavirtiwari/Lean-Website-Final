using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>News, announcements, circulars, tenders and success stories.</summary>
public class Post : AuditableEntity, IPublishable
{
    public PostType Type { get; set; } = PostType.News;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? Body { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Author { get; set; }

    /// <summary>Attached PDF/DOC for circulars and tenders.</summary>
    public string? AttachmentUrl { get; set; }
    public string? AttachmentLabel { get; set; }

    /// <summary>Shown in the scrolling "What's New" ticker on the home page.</summary>
    public bool IsFeatured { get; set; }
    public bool ShowInTicker { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public int ViewCount { get; set; }

    public string? MetaDescription { get; set; }
}
