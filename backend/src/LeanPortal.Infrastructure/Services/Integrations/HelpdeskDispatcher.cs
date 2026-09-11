using System.Text.Json;
using System.Text.Json.Nodes;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services.Integrations;

public interface IHelpdeskDispatcher
{
    /// <summary>
    /// The helpdesk an enquiry for this agency goes to, or null for an agency that
    /// works by e-mail. Only an enabled, complete connection counts.
    /// </summary>
    Task<HelpdeskConnection?> ConnectionForAsync(string? agency, CancellationToken ct);

    /// <summary>
    /// Raises the enquiry as a ticket. On failure, schedules the next attempt - or,
    /// once the schedule has run out, sends the enquiry to the agency's inbox so it
    /// is not left with nobody.
    /// </summary>
    Task<HelpdeskStatus> DispatchAsync(int messageId, CancellationToken ct);
}

/// <summary>
/// Turns an enquiry into a Zoho Desk ticket: finds or creates the contact, uploads
/// the attachments, then raises the ticket with them - the order Zoho's file
/// attachment note sets out, since a ticket cannot be given files after the fact
/// in one call.
///
/// Zoho's note has the browser upload each file the moment it is picked. Here the
/// files come to the portal with the form and are uploaded by the server straight
/// before the ticket is raised. The enquiry is in the database either way, so a
/// file never exists only in Zoho's 24-hour holding area, and there is no
/// half-finished upload to time out if someone leaves the form open.
/// </summary>
public class HelpdeskDispatcher(
    ApplicationDbContext db,
    IZohoDeskClient zoho,
    ISecretProtector secrets,
    IFileStorage storage,
    IEnquiryRouter router,
    IEmailSender email,
    ILogger<HelpdeskDispatcher> logger) : IHelpdeskDispatcher
{
    /// <summary>
    /// When to try again after each failure. Six attempts over roughly four hours:
    /// long enough to ride out a Zoho outage or a revoked token being replaced,
    /// short enough that a grievance is not left a day before someone sees it.
    /// </summary>
    public static readonly TimeSpan[] RetrySchedule =
    [
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
    ];

    /// <summary>What a ticket template may use. Shown in the console and checked on save.</summary>
    public static readonly IReadOnlyList<(string Name, string Meaning)> Placeholders =
    [
        ("name", "Full name"),
        ("firstName", "Every word of the name but the last"),
        ("lastName", "The last word of the name"),
        ("email", "E-mail address"),
        ("phone", "Mobile number"),
        ("organisation", "Enterprise / organisation"),
        ("udyamNumber", "Udyam registration number"),
        ("state", "State"),
        ("subject", "Subject"),
        ("message", "The message"),
        ("agency", "Agency chosen, e.g. QCI"),
        ("category", "Category, where no grievance matrix is used"),
        ("userType", "Grievance matrix: user type"),
        ("issueType", "Grievance matrix: sub-category I (complaint or query)"),
        ("issueCategory", "Grievance matrix: sub-category II"),
        ("issueSubCategory", "Grievance matrix: sub-category III"),
        ("reference", "The portal's own reference, e.g. LEAN-ENQ-000123"),
    ];

    /// <summary>
    /// What a new connection starts with: the NDIE department's fields from the
    /// SAMAR document, filled from the grievance matrix.
    /// </summary>
    public const string DefaultTicketTemplate = """
        {
          "cf": {
            "cf_user_role": "{{userType}}",
            "cf_organisation_name": "{{organisation}}",
            "cf_issue_type": "{{issueType}}",
            "cf_issue_category": "{{issueCategory}}",
            "cf_issue_sub_category": "{{issueSubCategory}}",
            "cf_udyam_number": "{{udyamNumber}}"
          },
          "priority": "Medium"
        }
        """;

    public async Task<HelpdeskConnection?> ConnectionForAsync(string? agency, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(agency)) return null;

        var connection = await db.HelpdeskConnections.AsNoTracking()
            .Include(c => c.Partner)
            .Where(c => c.IsEnabled && c.PartnerId != null)
            .FirstOrDefaultAsync(c =>
                c.Partner!.ShortName == agency || c.Partner.Name == agency, ct);

        return connection is not null && IsComplete(connection) ? connection : null;
    }

    public static bool IsComplete(HelpdeskConnection c) =>
        !string.IsNullOrWhiteSpace(c.OrganisationId) && !string.IsNullOrWhiteSpace(c.DepartmentId)
        && !string.IsNullOrWhiteSpace(c.ClientId) && !string.IsNullOrWhiteSpace(c.ClientSecret)
        && !string.IsNullOrWhiteSpace(c.RefreshToken);

    public async Task<HelpdeskStatus> DispatchAsync(int messageId, CancellationToken ct)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (message is null || message.HelpdeskStatus is HelpdeskStatus.Sent or HelpdeskStatus.NotApplicable)
            return message?.HelpdeskStatus ?? HelpdeskStatus.NotApplicable;

        var connection = await ConnectionForAsync(message.Agency, ct);
        if (connection is null)
        {
            // Switched off, or taken off this agency, while the enquiry was waiting.
            // It still has to reach someone, so it goes by mail now.
            await FallBackToMailAsync(message,
                "This enquiry was waiting to be raised in the helpdesk, which has since been switched off.", ct);
            message.HelpdeskStatus = HelpdeskStatus.NotApplicable;
            message.HelpdeskNextAttemptAt = null;
            await db.SaveChangesAsync(ct);
            return message.HelpdeskStatus;
        }

        message.HelpdeskAttempts++;

        try
        {
            var ticket = await RaiseAsync(connection, message, ct);

            message.HelpdeskStatus = HelpdeskStatus.Sent;
            message.HelpdeskTicketId = ticket.Id;
            message.HelpdeskTicketNumber = ticket.TicketNumber;
            message.HelpdeskSentAt = DateTimeOffset.UtcNow;
            message.HelpdeskNextAttemptAt = null;
            message.HelpdeskLastError = null;
            await db.SaveChangesAsync(CancellationToken.None);

            logger.LogInformation("Enquiry {Id} raised in Zoho Desk as {Ticket}",
                message.Id, ticket.TicketNumber ?? ticket.Id);

            if (connection.AlsoSendEmail)
                await SendMailAsync(message, null, CancellationToken.None);

            return message.HelpdeskStatus;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var reason = ex is ZohoDeskException or HttpRequestException or TaskCanceledException
                ? ex.Message
                : $"{ex.GetType().Name}: {ex.Message}";

            message.HelpdeskLastError = reason.Length > 1000 ? reason[..1000] : reason;

            if (message.HelpdeskAttempts > RetrySchedule.Length)
            {
                message.HelpdeskStatus = HelpdeskStatus.Failed;
                message.HelpdeskNextAttemptAt = null;
                await db.SaveChangesAsync(CancellationToken.None);

                logger.LogError(ex, "Enquiry {Id} could not be raised in Zoho Desk after {Attempts} attempts; " +
                                    "sending it to the agency inbox instead", message.Id, message.HelpdeskAttempts);

                await FallBackToMailAsync(message,
                    $"This enquiry could not be raised in the helpdesk after {message.HelpdeskAttempts} attempts " +
                    $"({message.HelpdeskLastError}). Please enter it there by hand.", CancellationToken.None);
            }
            else
            {
                message.HelpdeskStatus = HelpdeskStatus.Pending;
                message.HelpdeskNextAttemptAt = DateTimeOffset.UtcNow + RetrySchedule[message.HelpdeskAttempts - 1];
                await db.SaveChangesAsync(CancellationToken.None);

                logger.LogWarning(ex, "Enquiry {Id} not yet raised in Zoho Desk (attempt {Attempt}); retrying at {When}",
                    message.Id, message.HelpdeskAttempts, message.HelpdeskNextAttemptAt);
            }

            return message.HelpdeskStatus;
        }
    }

    private async Task<ZohoTicket> RaiseAsync(HelpdeskConnection connection, ContactMessage message, CancellationToken ct)
    {
        var account = AccountFor(connection, secrets);
        var values = ValuesFor(message);

        var contactId = await zoho.FindOrCreateContactAsync(account, new ZohoContact(
            values["firstName"], values["lastName"] ?? message.Name, message.Email, message.Phone,
            connection.ContactOwnerId), ct);

        var uploads = new JsonArray();
        if (connection.SendAttachments && !string.IsNullOrWhiteSpace(message.AttachmentsJson))
        {
            var files = JsonSerializer.Deserialize<List<StoredAttachment>>(message.AttachmentsJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];

            foreach (var file in files)
            {
                await using var stream = storage.OpenRead(file.Url)
                    ?? throw new ZohoDeskException($"The attachment {file.Name} is no longer on the server.");

                uploads.Add(await zoho.UploadAsync(account, stream, file.Name, ct));

                // One at a time, a little apart: Zoho's note warns against uploading in
                // parallel into its rate limit.
                await Task.Delay(TimeSpan.FromMilliseconds(300), ct);
            }
        }

        // Not cancellable from outside. The form gives up waiting after a few seconds,
        // but a ticket request already on its way may well succeed; abandoning it then
        // would retry later and raise the same enquiry twice. Zoho's own timeout on
        // the client still applies.
        return await zoho.CreateTicketAsync(account, BuildTicket(connection, message, contactId, uploads),
            CancellationToken.None);
    }

    /// <summary>
    /// The ticket body: the fields every Zoho ticket needs, then the console's
    /// template over the top. The template may change channel, priority or anything
    /// else, but never whose ticket it is or which files are on it.
    /// </summary>
    public static JsonObject BuildTicket(
        HelpdeskConnection connection, ContactMessage message, string contactId, JsonArray? uploads)
    {
        var values = ValuesFor(message);

        var ticket = new JsonObject
        {
            ["subject"] = message.Subject,
            ["description"] = Description(message),
            ["departmentId"] = connection.DepartmentId,
            ["channel"] = string.IsNullOrWhiteSpace(connection.Channel) ? "Web" : connection.Channel,
            ["email"] = message.Email,
            ["phone"] = message.Phone,
        };

        foreach (var (key, value) in JsonTemplate.Render(connection.TicketTemplate, values).ToList())
            ticket[key] = value?.DeepClone();

        ticket["contactId"] = contactId;
        if (uploads is { Count: > 0 }) ticket["uploads"] = uploads.DeepClone();

        return ticket;
    }

    public static Dictionary<string, string?> ValuesFor(ContactMessage message)
    {
        var words = (message.Name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Zoho requires a last name and has no field for a single name, so one word
        // is the last name.
        var last = words.Length > 0 ? words[^1] : message.Name;
        var first = words.Length > 1 ? string.Join(' ', words[..^1]) : null;

        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = message.Name,
            ["firstName"] = first,
            ["lastName"] = last,
            ["email"] = message.Email,
            ["phone"] = message.Phone,
            ["organisation"] = message.Organisation,
            ["udyamNumber"] = message.UdyamNumber,
            ["state"] = message.State,
            ["subject"] = message.Subject,
            ["message"] = message.Message,
            ["agency"] = message.Agency,
            ["category"] = message.Category,
            ["userType"] = message.UserType,
            ["issueType"] = message.IssueType,
            ["issueCategory"] = message.IssueCategory,
            ["issueSubCategory"] = message.IssueSubCategory,
            ["reference"] = message.Id > 0 ? EnquiryMail.Reference(message) : null,
        };
    }

    /// <summary>
    /// The message, then the details that have no field of their own in Zoho, so
    /// the agent reading the ticket has them without opening anything else.
    /// Zoho shows a description as HTML, so it is escaped and broken with tags.
    /// </summary>
    private static string Description(ContactMessage message)
    {
        var details = new[]
        {
            ("Portal reference", message.Id > 0 ? EnquiryMail.Reference(message) : null),
            ("Enterprise / organisation", message.Organisation),
            ("Udyam number", message.UdyamNumber),
            ("State", message.State),
        }.Where(d => !string.IsNullOrWhiteSpace(d.Item2))
         .Select(d => $"{d.Item1}: {EnquiryMail.Escape(d.Item2)}");

        return EnquiryMail.Escape(message.Message)!.Replace("\n", "<br/>") +
               "<br/><br/>---<br/>" + string.Join("<br/>", details) +
               "<br/>Submitted through the LEAN Scheme portal.";
    }

    public static ZohoAccount AccountFor(HelpdeskConnection c, ISecretProtector secrets)
    {
        var secret = secrets.Unprotect(c.ClientSecret);
        var refresh = secrets.Unprotect(c.RefreshToken);

        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(refresh))
            throw new ZohoDeskException(
                "The client secret or refresh token could not be decrypted - the server's keys have changed. " +
                "Enter them again under Helpdesk in the console.");

        return new ZohoAccount(c.Id, c.AccountsUrl, c.ApiBaseUrl, c.OrganisationId!, c.ClientId!, secret, refresh);
    }

    private async Task FallBackToMailAsync(ContactMessage message, string note, CancellationToken ct) =>
        await SendMailAsync(message, note, ct);

    private async Task SendMailAsync(ContactMessage message, string? note, CancellationToken ct)
    {
        var inbox = await router.ResolveAsync(message.Agency, ct);
        if (string.IsNullOrWhiteSpace(inbox))
        {
            logger.LogError("Enquiry {Id} for {Agency} has no inbox to fall back to; it is only in the database",
                message.Id, message.Agency);
            return;
        }

        try
        {
            await email.SendForAgencyAsync(message.Agency, inbox, EnquiryMail.Subject(message, note is null ? null : "ACTION NEEDED - "),
                EnquiryMail.Body(message, note), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not e-mail {Inbox} about enquiry {Id}", inbox, message.Id);
        }
    }

    private sealed record StoredAttachment(string Name, string Url, long SizeBytes);
}
