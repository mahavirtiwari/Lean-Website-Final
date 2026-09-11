using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>MSME success story shown in the testimonial carousel.</summary>
public class Testimonial : AuditableEntity, IPublishable
{
    public string UnitName { get; set; } = string.Empty;
    public string? PersonName { get; set; }
    public string? Designation { get; set; }
    public string? Location { get; set; }
    public string? Sector { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? VideoUrl { get; set; }
    public LeanLevel? AchievedLevel { get; set; }
    /// <summary>Headline outcome, e.g. "Rs. 18 lakh annual saving".</summary>
    public string? ImpactHighlight { get; set; }
    public int SortOrder { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Published;
    public DateTimeOffset? PublishedAt { get; set; }
}
