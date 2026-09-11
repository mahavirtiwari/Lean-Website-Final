using System.Net;
using System.Net.Mail;
using LeanPortal.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services;

/// <summary>
/// Sends mail through the configured relay.
///
/// The settings come from the console, falling back to the server's file, and are
/// read on every send rather than at start-up - an operator who corrects the
/// password in the console expects the next enquiry to go, not to have to ask for
/// a restart.
///
/// This matters more than it looks. Enquiries are not read in the console, so this
/// is the last link that reaches a person; a failure is thrown rather than
/// swallowed, and the caller records the enquiry before attempting to send.
/// </summary>
public class SmtpEmailSender(IMailRelayProvider relay, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default) =>
        await SendViaAsync(await relay.GetAsync(ct), to, subject, htmlBody, ct);

    public async Task SendForAgencyAsync(
        string? agency, string to, string subject, string htmlBody, CancellationToken ct = default) =>
        await SendViaAsync(await relay.GetForAgencyAsync(agency, ct), to, subject, htmlBody, ct);

    public async Task SendViaAsync(
        MailRelaySettings mail, string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mail.Host))
            throw new InvalidOperationException(
                "No mail server is configured. Set one in the console under Settings, or Smtp:Host on the server.");

        if (string.IsNullOrWhiteSpace(mail.FromAddress))
            throw new InvalidOperationException(
                "No 'send as' address is configured. A relay will refuse mail without one.");

        using var client = new SmtpClient(mail.Host)
        {
            Port = mail.Port,
            // SslOnConnect (port 465) is not supported by SmtpClient; StartTls is what
            // Microsoft 365 and almost every departmental relay use on 587.
            EnableSsl = !string.Equals(mail.Encryption, "None", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30_000,
            UseDefaultCredentials = false
        };

        // An internal relay that accepts anonymously needs no credentials, and
        // offering a blank user with a password set makes it authenticate with
        // nothing and be refused.
        if (!string.IsNullOrWhiteSpace(mail.User) && !string.IsNullOrWhiteSpace(mail.Password))
            client.Credentials = new NetworkCredential(mail.User, mail.Password);

        using var message = new MailMessage
        {
            From = new MailAddress(mail.FromAddress, mail.FromName ?? "MSME Competitive (LEAN) Scheme"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(to);

        // A copy for the ministry where one is configured, so the scheme team can see
        // what the agencies are being asked without reading the agencies' mail.
        if (!string.IsNullOrWhiteSpace(mail.CopyTo)) message.Bcc.Add(mail.CopyTo);

        await client.SendMailAsync(message, ct);
        logger.LogInformation("Sent \"{Subject}\" to {To} via {Host}", subject, to, mail.Host);
    }
}
