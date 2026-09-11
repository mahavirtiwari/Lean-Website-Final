using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A card in the Benefits / Incentives band on the home page.
///
/// Each one names a source of support - the ministry's own schemes, a state or
/// union territory, the banks, another central ministry - and links to the page
/// that sets out what is on offer. The categories are content, not code: which
/// bodies offer incentives changes, so they are rows an editor can add to,
/// reorder and take down rather than a fixed list in a template.
/// </summary>
public class Benefit : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>The line under the title, e.g. "Incentives by States / UTs".</summary>
    public string? Subtitle { get; set; }

    /// <summary>Uploaded artwork. Falls back to <see cref="Icon"/> when not set.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Name of a built-in icon, used when no artwork is uploaded.</summary>
    public string? Icon { get; set; }

    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }

    public bool OpenInNewTab { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
