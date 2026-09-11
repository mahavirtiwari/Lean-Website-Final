using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>Navigation entry. Either links to a CMS Page or to an explicit (often external) URL.</summary>
public class MenuItem : AuditableEntity
{
    public MenuLocation Location { get; set; } = MenuLocation.Main;
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int? PageId { get; set; }
    public Page? Page { get; set; }

    public int? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];

    public int SortOrder { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Icon { get; set; }
    /// <summary>Renders the item as a highlighted button (e.g. "Register for Lean Scheme").</summary>
    public bool IsHighlighted { get; set; }
}
