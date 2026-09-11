using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>Key/value site configuration editable by administrators (addresses, social URLs, toggles).</summary>
public class SiteSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    /// <summary>Grouping shown as a tab in the admin settings screen.</summary>
    public string Group { get; set; } = "General";
    /// <summary>
    /// Card within the tab, so related settings are edited together rather than as one long
    /// form. A section whose keys include a <c>feature.*</c> switch is shown with that switch
    /// in its header, which is how a part of the site is turned off.
    /// </summary>
    public string? Section { get; set; }
    /// <summary>Editor hint: text, textarea, html, url, email, image, boolean, number.</summary>
    public string DataType { get; set; } = "text";
    public int SortOrder { get; set; }
    /// <summary>Public settings are served to the Angular app unauthenticated.</summary>
    public bool IsPublic { get; set; } = true;
}
