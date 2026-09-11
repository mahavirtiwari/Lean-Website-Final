using LeanPortal.Domain.Entities;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>
/// The message an agency's inbox receives for an enquiry.
///
/// Built in one place because two paths send it: the form, for an agency that
/// works by e-mail, and the helpdesk dispatcher, when a ticket could not be raised
/// and the enquiry has to reach someone by the other route.
/// </summary>
public static class EnquiryMail
{
    public static string Reference(ContactMessage message) => $"LEAN-ENQ-{message.Id:D6}";

    public static string Subject(ContactMessage message, string? prefix = null) =>
        $"[LEAN Portal] {prefix}New enquiry: {message.Subject}";

    public static string Body(ContactMessage message, string? note = null)
    {
        static string Line(string label, string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : $"<strong>{label}:</strong> {Escape(value)}<br/>";

        var classification = string.Join(" › ", new[]
        {
            message.UserType, message.IssueType, message.IssueCategory, message.IssueSubCategory,
        }.Where(v => !string.IsNullOrWhiteSpace(v)));

        return
            (string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p><em>{Escape(note)}</em></p>") +
            "<p>A new enquiry has been submitted on the LEAN Scheme portal.</p>" +
            "<p>" +
            Line("Reference", Reference(message)) +
            Line("Helpdesk ticket", message.HelpdeskTicketNumber) +
            Line("For", message.Agency ?? "the scheme team") +
            $"<strong>From:</strong> {Escape(message.Name)} &lt;{Escape(message.Email)}&gt;<br/>" +
            Line("Mobile", message.Phone) +
            Line("Enterprise", message.Organisation) +
            Line("Udyam", message.UdyamNumber) +
            Line("State", message.State) +
            Line("Classification", classification) +
            Line("Category", string.IsNullOrWhiteSpace(classification) ? message.Category ?? "General" : null) +
            "</p>" +
            $"<p><strong>Subject:</strong> {Escape(message.Subject)}</p>" +
            $"<p>{Escape(message.Message)?.Replace("\n", "<br/>")}</p>" +
            $"<p>Reply to the sender at {Escape(message.Email)}.</p>";
    }

    /// <summary>
    /// Everything here came from a public form and goes into HTML: without this, a
    /// sender could put markup - or a link - into mail that lands in an officer's
    /// inbox looking as though the portal wrote it.
    /// </summary>
    public static string? Escape(string? value) =>
        string.IsNullOrEmpty(value)
            ? value
            : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
