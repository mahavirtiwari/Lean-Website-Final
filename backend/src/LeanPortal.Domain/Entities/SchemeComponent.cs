using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>One of the six scheme components rendered as an icon tile grid.</summary>
public class SchemeComponent : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
