using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>Bronze / Silver / Gold tier of the MSME Competitive (LEAN) Scheme.</summary>
public class SchemeLevel : AuditableEntity
{
    public LeanLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Bronze / Silver / Gold style badge label.</summary>
    public string? BadgeLabel { get; set; }
    public string? Tagline { get; set; }
    public string? Description { get; set; }
    /// <summary>Newline-separated deliverables rendered as a tick list.</summary>
    public string? Deliverables { get; set; }
    public string? FeeStructure { get; set; }
    public string? Duration { get; set; }
    public string? IconUrl { get; set; }
    public string? CertificateImageUrl { get; set; }
    /// <summary>Accent colour (hex) used for the level card.</summary>
    public string? AccentColor { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
