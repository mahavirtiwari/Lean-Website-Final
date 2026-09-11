using LeanPortal.Application.Interfaces;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services;

/// <summary>
/// Encrypts the values that must not be readable in the database.
///
/// The mail password is configured in the console, which means it is stored - so
/// it is stored encrypted. The key comes from ASP.NET data protection, kept
/// outside the database, so a copy of the database on its own does not hand over
/// the mailbox.
///
/// Losing the key ring means the password cannot be decrypted and has to be typed
/// again. That is the right way round: it fails closed, and re-entering a password
/// is a minute's work.
/// </summary>
public class SecretProtector(IDataProtectionProvider provider, ILogger<SecretProtector> logger) : ISecretProtector
{
    private const string Marker = "enc:v1:";

    private IDataProtector Protector => provider.CreateProtector("LeanPortal.Secrets.v1");

    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        // Already encrypted - saving a settings tab twice must not double-wrap it.
        return plaintext.StartsWith(Marker, StringComparison.Ordinal)
            ? plaintext
            : Marker + Protector.Protect(plaintext);
    }

    public string? Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return stored;
        if (!stored.StartsWith(Marker, StringComparison.Ordinal)) return stored;

        try
        {
            return Protector.Unprotect(stored[Marker.Length..]);
        }
        catch (Exception ex)
        {
            // A rotated or lost key ring. Say so rather than sending mail with a
            // password that is really ciphertext and failing at the relay.
            logger.LogError(ex, "Could not decrypt a stored secret; it must be entered again.");
            return null;
        }
    }

    public bool IsProtected(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith(Marker, StringComparison.Ordinal);
}

/// <summary>
/// The mail relay, taken from the console first and the server's configuration
/// second.
///
/// Both are supported on purpose. The ministry asked to configure mail in the
/// console, and that is where it is read from; the file remains as a way to set it
/// before anyone can sign in, and for a deployment that would rather keep the
/// password out of the database entirely.
/// </summary>
public class MailRelayProvider(
    ApplicationDbContext db,
    IConfiguration config,
    ISecretProtector secrets) : IMailRelayProvider
{
    public async Task<MailRelaySettings> GetAsync(CancellationToken ct = default)
    {
        var rows = await db.SiteSettings.AsNoTracking()
            .Where(s => s.Key.StartsWith("mail."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        string? FromConsole(string key) =>
            rows.TryGetValue("mail." + key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;

        string? Setting(string key, string configKey) =>
            FromConsole(key) ?? (string.IsNullOrWhiteSpace(config[configKey]) ? null : config[configKey]!.Trim());

        var port = Setting("port", "Smtp:Port");
        var encryption = Setting("encryption", "Smtp:Encryption")
                         ?? (bool.TryParse(config["Smtp:EnableSsl"], out var ssl) && !ssl ? "None" : "StartTls");

        // The console's password is encrypted; the file's is not, because a file the
        // operator controls is already outside the database.
        var password = FromConsole("password") is { } stored
            ? secrets.Unprotect(stored)
            : config["Smtp:Password"];

        return new MailRelaySettings(
            Host: Setting("host", "Smtp:Host"),
            Port: int.TryParse(port, out var parsed) ? parsed : 587,
            Encryption: encryption,
            User: Setting("username", "Smtp:User"),
            Password: password,
            FromAddress: Setting("fromAddress", "Smtp:FromAddress"),
            FromName: Setting("fromName", "Smtp:FromName") ?? "MSME Competitive (LEAN) Scheme",
            CopyTo: Setting("copyTo", "Smtp:CopyTo"));
    }

    public async Task<MailRelaySettings> GetForAgencyAsync(string? agency, CancellationToken ct = default)
    {
        var portal = await GetAsync(ct);
        if (string.IsNullOrWhiteSpace(agency)) return portal;

        var own = await db.AgencyMailRelays.AsNoTracking()
            .Where(r => r.UseOwnServer && (r.Partner!.ShortName == agency || r.Partner.Name == agency))
            .FirstOrDefaultAsync(ct);

        if (own is null || string.IsNullOrWhiteSpace(own.Host) || string.IsNullOrWhiteSpace(own.FromAddress))
            return portal;

        // The ministry's blind copy stays whichever server the mail leaves from: it
        // is how the scheme team sees what the agencies are asked.
        return new MailRelaySettings(
            Host: own.Host.Trim(),
            Port: own.Port > 0 ? own.Port : 587,
            Encryption: string.IsNullOrWhiteSpace(own.Encryption) ? "StartTls" : own.Encryption,
            User: own.Username?.Trim(),
            Password: secrets.Unprotect(own.Password),
            FromAddress: own.FromAddress.Trim(),
            FromName: string.IsNullOrWhiteSpace(own.FromName) ? portal.FromName : own.FromName.Trim(),
            CopyTo: portal.CopyTo);
    }
}
