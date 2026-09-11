using LeanPortal.Application.Contracts;

namespace LeanPortal.Application.Interfaces;

/// <summary>Issues and validates the JWT access/refresh token pair used by the admin console.</summary>
public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(string userId, string email, string fullName,
        IEnumerable<string> roles, bool mustChangePassword = false);

    string CreateRefreshToken();
}

/// <summary>Stores uploaded files and returns the public URL under which they are served.</summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, string folder,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string url, CancellationToken ct = default);

    /// <summary>Opens a stored upload by its public address, or null if there is no such file.</summary>
    Stream? OpenRead(string url);

    /// <summary>True when the extension and content type are on the allow-list for the given folder.</summary>
    bool IsAllowed(string fileName, string contentType, out string? reason);
}

public record StoredFile(string Url, string StoredFileName, long SizeBytes, string ContentType,
    int? Width = null, int? Height = null, string? Checksum = null);

/// <summary>Issues and checks the character challenge on the public forms.</summary>
public interface ICaptchaService
{
    CaptchaChallenge Issue();

    /// <summary>
    /// The same check as a written question - a small sum - for anyone who cannot
    /// see the picture. GIGW and WCAG 1.1.1 both require a CAPTCHA to offer a form
    /// that does not depend on sight.
    /// </summary>
    CaptchaChallenge IssueQuestion();

    /// <summary>True when the answer matches. The challenge is spent either way.</summary>
    bool Validate(string? id, string? answer);
}

/// <summary>An identifier to send back with the answer, and the picture - or the question - to show.</summary>
public record CaptchaChallenge(string Id, string? Svg, string? Question = null);

/// <summary>Strips scripting and unsafe markup from rich-text submitted by editors.</summary>
public interface IContentSanitizer
{
    string? Sanitize(string? html);
}

/// <summary>Writes the immutable admin audit trail.</summary>
public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId, object? changes = null,
        CancellationToken ct = default);
}

/// <summary>Sends transactional e-mail (enquiry acknowledgements, newsletter confirmations).</summary>
/// <summary>Encrypts values that must not be readable in the database.</summary>
public interface ISecretProtector
{
    string? Protect(string? plaintext);
    string? Unprotect(string? stored);
    bool IsProtected(string? value);
}

/// <summary>
/// The mail relay's settings, wherever they were configured.
/// </summary>
public record MailRelaySettings(
    string? Host,
    int Port,
    string Encryption,
    string? User,
    string? Password,
    string? FromAddress,
    string? FromName,
    string? CopyTo)
{
    /// <summary>Enough to send with. Without an address to send from, a relay will refuse.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>Reads the mail relay, from the console first and the server's file second.</summary>
public interface IMailRelayProvider
{
    /// <summary>The portal's own mail server.</summary>
    Task<MailRelaySettings> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// The server an agency's enquiries leave through: its own when it has one
    /// switched on and complete, the portal's otherwise.
    /// </summary>
    Task<MailRelaySettings> GetForAgencyAsync(string? agency, CancellationToken ct = default);
}

/// <summary>Decides which inbox an enquiry belongs in.</summary>
public interface IEnquiryRouter
{
    /// <summary>
    /// The address for an enquiry chosen for <paramref name="agency"/>: that
    /// agency's own inbox, then the address it publishes, then the site-wide one.
    /// Null when none of the three is set.
    /// </summary>
    Task<string?> ResolveAsync(string? agency, CancellationToken ct = default);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>Sends through the agency's own mail server when it has one, the portal's otherwise.</summary>
    Task SendForAgencyAsync(string? agency, string to, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>Sends through exactly this server - for a test from the console.</summary>
    Task SendViaAsync(MailRelaySettings relay, string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Cached read access to the public site settings.</summary>
public interface ISiteSettingsProvider
{
    Task<IReadOnlyDictionary<string, string?>> GetPublicSettingsAsync(CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Reads a boolean feature toggle. Missing or unparsable means <paramref name="fallback"/>.</summary>
    Task<bool> IsEnabledAsync(string key, CancellationToken ct = default, bool fallback = true);
    void Invalidate();
}
