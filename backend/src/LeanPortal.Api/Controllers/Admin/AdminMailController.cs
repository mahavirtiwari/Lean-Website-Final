using System.ComponentModel.DataAnnotations;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Admin;

/// <summary>
/// Enquiry mail, in one place: the portal's mail server, each agency's inbox, and
/// - for an agency that wants its enquiries to leave from its own mailbox - a
/// server of its own. And a way to prove any of them works.
///
/// Passwords are write-only: stored encrypted, never sent back, kept as they are
/// when the box is left empty. A server-side Smtp section in the configuration file
/// still works for the portal's server when the console leaves it empty.
/// </summary>
[Route("admin/mail")]
// CanAdminister, not CanEdit: this holds mail passwords and every agency's inbox,
// and can make the server send mail. An editor has no business with any of it.
[Authorize(Policy = Policies.CanAdminister)]
public class AdminMailController(
    ApplicationDbContext db,
    IMailRelayProvider relay,
    IEmailSender email,
    ISecretProtector secrets,
    ISiteSettingsProvider settings,
    IAuditService audit,
    ILogger<AdminMailController> logger) : ApiControllerBase(db)
{
    private static readonly string[] PortalKeys =
        ["mail.host", "mail.port", "mail.encryption", "mail.username", "mail.password", "mail.fromAddress", "mail.fromName", "mail.copyTo"];

    /// <summary>Everything the screen edits.</summary>
    [HttpGet("config")]
    [ProducesResponseType<MailConfigDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MailConfigDto>> GetConfig(CancellationToken ct)
    {
        var rows = await Db.SiteSettings.AsNoTracking()
            .Where(s => PortalKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        string? Get(string key) => rows.GetValueOrDefault(key);

        var portal = new MailServerDto(
            Get("mail.host"),
            int.TryParse(Get("mail.port"), out var port) ? port : 587,
            Get("mail.encryption") ?? "StartTls",
            Get("mail.username"),
            !string.IsNullOrEmpty(Get("mail.password")),
            Get("mail.fromAddress"),
            Get("mail.fromName"));

        var relays = await Db.AgencyMailRelays.AsNoTracking().ToDictionaryAsync(r => r.PartnerId, ct);
        var helpdesk = await Db.HelpdeskConnections.AsNoTracking()
            .Where(c => c.IsEnabled && c.PartnerId != null)
            .Select(c => c.PartnerId!.Value)
            .ToListAsync(ct);

        var agencies = await Db.Partners.AsNoTracking()
            .Where(p => p.Type == PartnerType.ImplementationAgency && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return Ok(new MailConfigDto(portal, Get("mail.copyTo"), agencies.Select(a =>
        {
            var own = relays.GetValueOrDefault(a.Id);
            return new AgencyMailDto(
                a.Id, a.Name, a.ShortName, a.EnquiryEmail, a.Email,
                own?.UseOwnServer ?? false,
                new MailServerDto(own?.Host, own?.Port ?? 587, own?.Encryption ?? "StartTls", own?.Username,
                    !string.IsNullOrEmpty(own?.Password), own?.FromAddress, own?.FromName),
                helpdesk.Contains(a.Id));
        }).ToList()));
    }

    [HttpPut("config")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SaveConfig([FromBody] MailConfigSaveDto request, CancellationToken ct)
    {
        if (Check("The portal's mail server", request.Portal, required: false) is { } portalProblem)
            return BadRequestProblem(portalProblem);
        if (!IsEmailOrEmpty(request.CopyTo))
            return BadRequestProblem("The blind copy address is not a valid e-mail address.");

        var partners = await Db.Partners
            .Where(p => p.Type == PartnerType.ImplementationAgency)
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var agency in request.Agencies)
        {
            if (!partners.TryGetValue(agency.PartnerId, out var partner))
                return BadRequestProblem("One of the agencies is not an implementing agency.");

            var name = partner.ShortName ?? partner.Name;
            if (!IsEmailOrEmpty(agency.EnquiryEmail))
                return BadRequestProblem($"{name}: the enquiries inbox is not a valid e-mail address.");
            if (Check($"{name}'s mail server", agency.Server, required: agency.UseOwnServer) is { } problem)
                return BadRequestProblem(problem);
        }

        // ---------------------------------------------------------------- portal ----
        var rows = await Db.SiteSettings.Where(s => PortalKeys.Contains(s.Key)).ToDictionaryAsync(s => s.Key, ct);
        void Put(string key, string? value)
        {
            if (rows.TryGetValue(key, out var row)) row.Value = value?.Trim() ?? string.Empty;
        }

        Put("mail.host", request.Portal.Host);
        Put("mail.port", (request.Portal.Port > 0 ? request.Portal.Port : 587).ToString());
        Put("mail.encryption", Encryption(request.Portal.Encryption));
        Put("mail.username", request.Portal.Username);
        Put("mail.fromAddress", request.Portal.FromAddress);
        Put("mail.fromName", request.Portal.FromName);
        Put("mail.copyTo", request.CopyTo);
        if (!string.IsNullOrWhiteSpace(request.Portal.Password))
            Put("mail.password", secrets.Protect(request.Portal.Password.Trim()));

        // -------------------------------------------------------------- agencies ----
        var relays = await Db.AgencyMailRelays.ToDictionaryAsync(r => r.PartnerId, ct);
        foreach (var agency in request.Agencies)
        {
            partners[agency.PartnerId].EnquiryEmail = Clean(agency.EnquiryEmail);

            if (!relays.TryGetValue(agency.PartnerId, out var own))
            {
                // Nothing to keep for an agency that has never had a server of its own.
                if (!agency.UseOwnServer && string.IsNullOrWhiteSpace(agency.Server.Host)) continue;
                own = new AgencyMailRelay { PartnerId = agency.PartnerId };
                Db.AgencyMailRelays.Add(own);
            }

            own.UseOwnServer = agency.UseOwnServer;
            own.Host = Clean(agency.Server.Host);
            own.Port = agency.Server.Port > 0 ? agency.Server.Port : 587;
            own.Encryption = Encryption(agency.Server.Encryption);
            own.Username = Clean(agency.Server.Username);
            own.FromAddress = Clean(agency.Server.FromAddress);
            own.FromName = Clean(agency.Server.FromName);
            if (!string.IsNullOrWhiteSpace(agency.Server.Password))
                own.Password = secrets.Protect(agency.Server.Password.Trim());
        }

        await Db.SaveChangesAsync(ct);
        settings.Invalidate();

        // Which parts changed, never the addresses or passwords themselves.
        await audit.LogAsync("UPDATE", "Mail", null, new
        {
            portalPasswordChanged = !string.IsNullOrWhiteSpace(request.Portal.Password),
            agencies = request.Agencies.Select(a => new
            {
                a.PartnerId, a.UseOwnServer, passwordChanged = !string.IsNullOrWhiteSpace(a.Server.Password),
            }),
        }, ct);

        return NoContent();
    }

    /// <summary>
    /// Sends a test, through the portal's server or through the server an agency's
    /// enquiries would use - which proves the path an enquiry actually takes.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SendTest([FromBody] MailTestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequestProblem("Enter the address to send the test to.");

        string? agency = null;
        if (request.PartnerId is { } partnerId)
        {
            agency = await Db.Partners.Where(p => p.Id == partnerId).Select(p => p.ShortName ?? p.Name).FirstOrDefaultAsync(ct);
            if (agency is null) return BadRequestProblem("There is no such agency.");
        }

        var mail = await relay.GetForAgencyAsync(agency, ct);
        if (!mail.IsConfigured)
            return BadRequestProblem(
                "No mail server is set up, so nothing would be sent. Fill in the portal's mail server above " +
                "- the server and the 'send as' address are the two it needs.");

        try
        {
            await email.SendViaAsync(
                mail,
                request.To.Trim(),
                "[LEAN Portal] Test message",
                "<p>This is a test from the LEAN Scheme portal.</p>" +
                $"<p>It was sent through {EscapeHtml(mail.Host)} as {EscapeHtml(mail.FromAddress)}" +
                (agency is null ? "" : $", the server {EscapeHtml(agency)}'s enquiries use") +
                ". If you are reading it, that route works.</p>",
                ct);

            await audit.LogAsync("TEST_EMAIL", "Mail", null, new { to = request.To, agency }, ct);
            return Ok(new { message = $"Sent through {mail.Host} as {mail.FromAddress}. Check {request.To.Trim()}." });
        }
        catch (Exception ex)
        {
            // The message is shown to an administrator who can act on it, so the
            // reason matters more than tidiness here.
            logger.LogError(ex, "Test message to {To} via {Host} failed", request.To, mail.Host);
            return BadRequestProblem($"{mail.Host} refused it: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------- helpers ----

    private static string? Check(string what, MailServerSaveDto server, bool required)
    {
        if (required && (string.IsNullOrWhiteSpace(server.Host) || string.IsNullOrWhiteSpace(server.FromAddress)))
            return $"{what}: the server and the 'send as' address are both needed.";
        if (!string.IsNullOrWhiteSpace(server.Host) && server.Host.Trim().Any(c => char.IsWhiteSpace(c) || c is '/' or ':'))
            return $"{what}: the server is a host name only, e.g. smtp.office365.com - no https:// and no port.";
        if (server.Port is < 0 or > 65535)
            return $"{what}: the port must be a number such as 587.";
        if (!IsEmailOrEmpty(server.FromAddress))
            return $"{what}: the 'send as' address is not a valid e-mail address.";
        return null;
    }

    private static bool IsEmailOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || new EmailAddressAttribute().IsValid(value.Trim());

    private static string Encryption(string? value) =>
        string.Equals(value, "None", StringComparison.OrdinalIgnoreCase) ? "None" : "StartTls";

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeHtml(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
