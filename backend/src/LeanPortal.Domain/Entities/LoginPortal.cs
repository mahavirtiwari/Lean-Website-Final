using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A stakeholder login/registration tile (MSME, Implementation Agency, Ministry, State DFO,
/// Consultant, OEM). These deep-link into the existing transactional LEAN application.
/// </summary>
public class LoginPortal : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Audience { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }

    public string? LoginUrl { get; set; }
    public string? LoginText { get; set; } = "Login Now";
    public string? RegisterUrl { get; set; }
    public string? RegisterText { get; set; }

    public string? AccentColor { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool OpenInNewTab { get; set; } = true;
}
