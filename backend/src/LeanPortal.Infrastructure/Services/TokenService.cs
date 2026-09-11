using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LeanPortal.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeanPortal.Infrastructure.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "LeanPortal";
    public string Audience { get; set; } = "LeanPortalAdmin";

    /// <summary>
    /// Signing key. Must be supplied through configuration (user-secrets, environment variable or
    /// the Windows Server machine-level environment) - never committed to source control.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>How long one access token lasts. Short, so a copied one is worth little.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// How long a console session may sit unused before it ends. Enforced on the
    /// server by the refresh token's expiry, which each renewal moves forward.
    /// </summary>
    public int SessionIdleMinutes { get; set; } = 30;
}

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(
        string userId, string email, string fullName, IEnumerable<string> roles,
        bool mustChangePassword = false)
    {
        if (string.IsNullOrWhiteSpace(_options.Key) || Encoding.UTF8.GetByteCount(_options.Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 bytes. Configure a strong signing key before starting the API.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email),
            new("full_name", fullName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Carried in the token so the server can refuse the rest of the console until
        // the password is changed. The front end has its own guard, but a guard in the
        // browser stops nobody who calls the API directly.
        if (mustChangePassword) claims.Add(new Claim("must_change_password", "true"));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
