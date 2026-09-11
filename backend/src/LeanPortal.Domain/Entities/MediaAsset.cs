using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>An uploaded file in the media library.</summary>
public class MediaAsset : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string? Folder { get; set; } = "general";
    public string? Checksum { get; set; }
}
