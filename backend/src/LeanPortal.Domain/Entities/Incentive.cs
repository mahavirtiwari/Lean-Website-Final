using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// One incentive available to a LEAN enterprise, listed under the Benefits /
/// Incentives section.
///
/// Each entry names the body offering it, what it is worth, who to contact, and
/// where the notification can be read. That shape is deliberate: an enterprise
/// reading this needs to know whose scheme it is and who to ask, because almost
/// none of these are administered by this portal.
///
/// A null <see cref="Level"/> means the incentive applies whatever level the
/// enterprise has reached, and a null <see cref="State"/> that it is not limited
/// to one state. Both are filters on the public page rather than requirements.
/// </summary>
public class Incentive : AuditableEntity
{
    public IncentiveCategory Category { get; set; }

    /// <summary>Scheme level this is tied to, or null when it applies to all of them.</summary>
    public string? Level { get; set; }

    /// <summary>State or union territory, for incentives notified by one of them.</summary>
    public string? State { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>The body offering it - a ministry, a state department, a bank.</summary>
    public string? IssuerName { get; set; }
    public string? IssuerLogoUrl { get; set; }

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    /// <summary>Notifications, circulars or forms. Two are enough for every entry so far.</summary>
    public string? Document1Url { get; set; }
    public string? Document1Label { get; set; }
    public string? Document2Url { get; set; }
    public string? Document2Label { get; set; }

    /// <summary>An explanatory film, as a YouTube or Vimeo address.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Where an enterprise goes to claim it. No button is shown when unset.</summary>
    public string? AvailUrl { get; set; }
    public string? AvailLabel { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
