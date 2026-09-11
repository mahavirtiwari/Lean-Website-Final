using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>Frequently asked question, rendered in the accordion on the FAQs page.</summary>
public class Faq : AuditableEntity, IPublishable
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Category { get; set; } = "General";
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public int HelpfulCount { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Published;
    public DateTimeOffset? PublishedAt { get; set; }
}
