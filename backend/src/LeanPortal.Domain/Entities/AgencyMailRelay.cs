using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A mail server of an implementing agency's own, for the enquiries sent to it.
///
/// Optional. Without one - or with it switched off - the agency's enquiries go
/// through the portal's own server, as they always have. With one, they leave from
/// the agency's mailbox, so its staff see them as their own organisation's mail and
/// can answer from the same account.
/// </summary>
public class AgencyMailRelay : AuditableEntity
{
    public int PartnerId { get; set; }
    public Partner? Partner { get; set; }

    public bool UseOwnServer { get; set; }

    public string? Host { get; set; }
    public int Port { get; set; } = 587;

    /// <summary>StartTls or None.</summary>
    public string Encryption { get; set; } = "StartTls";

    public string? Username { get; set; }

    /// <summary>Encrypted at rest; never sent to the browser.</summary>
    public string? Password { get; set; }

    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
}
