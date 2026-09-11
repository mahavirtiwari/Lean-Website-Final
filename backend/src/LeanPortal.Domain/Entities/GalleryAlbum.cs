using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

public class GalleryAlbum : AuditableEntity, IPublishable
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset? EventDate { get; set; }
    public int SortOrder { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Published;
    public DateTimeOffset? PublishedAt { get; set; }

    public ICollection<GalleryImage> Images { get; set; } = [];
}

public class GalleryImage : AuditableEntity
{
    public int AlbumId { get; set; }
    public GalleryAlbum? Album { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    /// <summary>
    /// Set when the item is a video rather than a photograph. The image then acts as its
    /// poster, so a gallery can hold both without a second table to keep in step.
    /// </summary>
    public string? VideoUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    /// <summary>Off takes the item out of the album and the home band without deleting it.</summary>
    public bool IsActive { get; set; } = true;
}
