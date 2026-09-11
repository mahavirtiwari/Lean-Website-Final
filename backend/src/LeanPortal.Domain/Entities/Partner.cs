using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// Implementation agencies (QCI, NPC), industry associations, OEMs, empanelled consultant
/// organisations and the "Useful Links" tiles - one table, discriminated by <see cref="Type"/>.
/// </summary>
public class Partner : AuditableEntity
{
    public PartnerType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? ContactUrl { get; set; }
    /// <summary>Address published on the site for this body.</summary>
    public string? Email { get; set; }

    /// <summary>
    /// Where enquiries chosen for this agency are sent.
    ///
    /// Separate from <see cref="Email"/> on purpose: the address published on the
    /// page is often a person, while enquiries need to reach a monitored inbox that
    /// does not change when that person moves on. Falls back to Email, and then to
    /// the site-wide address, so an agency that has not set one still gets its post.
    ///
    /// Never leaves the server - it is routing, not content, and publishing it would
    /// hand a spam list to anyone reading the page source.
    /// </summary>
    public string? EnquiryEmail { get; set; }

    /// <summary>
    /// A form hosted by this agency - Zoho or anything else that gives an embed
    /// address - shown in place of the built-in form once this agency is chosen.
    /// The two agencies can use different ones, or one can and the other not.
    /// </summary>
    public string? EnquiryFormUrl { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    /// <summary>Empanelment / registration number where applicable.</summary>
    public string? RegistrationNumber { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
}
