using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A Zoho Desk account that enquiries for one implementing agency are raised in
/// as tickets.
///
/// One agency, not all of them: QCI runs its grievances through Zoho Desk and NPC
/// answers by e-mail, so an enquiry for any agency other than <see cref="PartnerId"/>
/// never comes near this. The agency is a setting rather than a constant so that
/// changes the day another agency joins, without a release.
///
/// Everything Zoho documents as varying between accounts - the data centre, the
/// organisation, the department, the channel and the custom fields a ticket
/// carries - is configuration here. The shape of the Zoho API itself is code.
/// </summary>
public class HelpdeskConnection : AuditableEntity
{
    public bool IsEnabled { get; set; }

    /// <summary>The implementing agency whose enquiries become tickets.</summary>
    public int? PartnerId { get; set; }
    public Partner? Partner { get; set; }

    /// <summary>Zoho's accounts server for this data centre, e.g. https://accounts.zoho.in.</summary>
    public string AccountsUrl { get; set; } = "https://accounts.zoho.in";

    /// <summary>The Desk API for the same data centre, e.g. https://desk.zoho.in.</summary>
    public string ApiBaseUrl { get; set; } = "https://desk.zoho.in";

    public string? OrganisationId { get; set; }
    public string? DepartmentId { get; set; }

    public string? ClientId { get; set; }

    /// <summary>Encrypted at rest; never sent to the browser.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Encrypted at rest; never sent to the browser.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Sent as the ticket's channel. Zoho's document for SAMAR says "Web".</summary>
    public string Channel { get; set; } = "Web";

    /// <summary>The agent a new Zoho contact is owned by, where the account wants one.</summary>
    public string? ContactOwnerId { get; set; }

    /// <summary>
    /// JSON merged into every ticket, with <c>{{placeholders}}</c> filled from the
    /// enquiry - which is where Zoho's custom fields go. Kept as a template so a
    /// field renamed or added in Zoho is a change in the console, not in code.
    /// </summary>
    public string? TicketTemplate { get; set; }

    /// <summary>
    /// How this agency classifies an enquiry: the grievance matrix, as JSON - a
    /// label for each level and a tree of options. The contact form shows it as
    /// linked drop-downs once this agency is chosen, and the ticket carries the
    /// choices in its custom fields.
    ///
    /// Here rather than in the site settings because it is the agency's, not the
    /// portal's: its values have to match the ones Zoho's custom fields accept.
    /// </summary>
    public string? GrievanceMatrix { get; set; }

    /// <summary>Uploads the enquiry's attachments to Zoho and links them to the ticket.</summary>
    public bool SendAttachments { get; set; } = true;

    /// <summary>
    /// Also e-mails the agency's inbox when the ticket is raised. Off by default:
    /// with Zoho doing the job, a second copy by mail is a second queue to work.
    /// </summary>
    public bool AlsoSendEmail { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }
    public bool? LastCheckSucceeded { get; set; }
    public string? LastCheckResult { get; set; }
}
