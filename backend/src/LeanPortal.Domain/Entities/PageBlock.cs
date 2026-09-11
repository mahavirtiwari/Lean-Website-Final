using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A composable section on a Blocks-template page. The home page is built entirely from these,
/// so editors can re-order, hide, or re-title every band of the landing page without a deploy.
/// </summary>
public class PageBlock : AuditableEntity
{
    public int PageId { get; set; }
    public Page? Page { get; set; }

    public BlockType Type { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public string? Eyebrow { get; set; }
    public string? Heading { get; set; }
    public string? SubHeading { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? PrimaryLinkText { get; set; }
    public string? PrimaryLinkUrl { get; set; }
    public string? SecondaryLinkText { get; set; }
    public string? SecondaryLinkUrl { get; set; }

    /// <summary>Free-form JSON for block-specific settings (column count, theme, filters).</summary>
    public string? SettingsJson { get; set; }
}
