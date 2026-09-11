using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A public "Get in touch" / grievance submission. Kept in the database and sent on
/// to the agency the sender chose - by e-mail, or as a helpdesk ticket for an
/// agency that runs one.
/// </summary>
public class ContactMessage : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Organisation { get; set; }
    public string? UdyamNumber { get; set; }
    public string? State { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>Enquiry category chosen in the form (Scheme, Registration, Technical, Grievance).</summary>
    public string? Category { get; set; }
    /// <summary>
    /// The implementing agency the sender picked, where the enquiry is for one of them
    /// rather than for the ministry. Recorded so the console can route it without
    /// reading the message.
    /// </summary>
    public string? Agency { get; set; }
    /// <summary>Files the sender attached, as a JSON list of name, address and size.</summary>
    public string? AttachmentsJson { get; set; }

    // --------------------------------------------------------- grievance matrix ----
    // The four levels of the grievance matrix, for an agency that classifies its
    // enquiries that way. Stored as the words the sender chose, not as positions in
    // the matrix, so an enquiry still reads correctly after the matrix is edited.

    /// <summary>Who is writing - MSME, and so on.</summary>
    public string? UserType { get; set; }
    /// <summary>Complaint or query: the matrix's first sub-category.</summary>
    public string? IssueType { get; set; }
    /// <summary>What it is about: the second.</summary>
    public string? IssueCategory { get; set; }
    /// <summary>The specific issue: the third.</summary>
    public string? IssueSubCategory { get; set; }

    // ----------------------------------------------------------------- helpdesk ----

    public HelpdeskStatus HelpdeskStatus { get; set; } = HelpdeskStatus.NotApplicable;
    /// <summary>Zoho's own id for the ticket.</summary>
    public string? HelpdeskTicketId { get; set; }
    /// <summary>The number Zoho shows people, e.g. NDIE-5300-260925.</summary>
    public string? HelpdeskTicketNumber { get; set; }
    public int HelpdeskAttempts { get; set; }
    public DateTimeOffset? HelpdeskNextAttemptAt { get; set; }
    public DateTimeOffset? HelpdeskSentAt { get; set; }
    public string? HelpdeskLastError { get; set; }

    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;
    public string? AssignedTo { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
