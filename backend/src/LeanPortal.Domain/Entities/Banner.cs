using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>A hero slide on the home page carousel.</summary>
public class Banner : AuditableEntity
{
    public string? Eyebrow { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? HighlightedTitle { get; set; }
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    /// <summary>Alt text - required for GIGW / WCAG compliance.</summary>
    public string? AltText { get; set; }

    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}
