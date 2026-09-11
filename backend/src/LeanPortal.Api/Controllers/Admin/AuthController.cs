using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Identity;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeanPortal.Api.Controllers.Admin;

/// <summary>Sign-in, token refresh and password management for CMS back-office users.</summary>
[Route("auth")]
[OutputCache(NoStore = true)]
public class AuthController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ITokenService tokens,
    IOptions<JwtOptions> jwtOptions,
    IAuditService audit,
    ICaptchaService captcha,
    ILogger<AuthController> logger) : ApiControllerBase(db)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // First, before the account is looked at: a failed challenge costs nothing
        // and tells nothing about whether the address has an account. With it, a
        // password cannot be tried by a script at the rate the limiter allows.
        if (!captcha.Validate(request.CaptchaId, request.CaptchaAnswer))
            return BadRequestProblem("The verification did not match. Please try the new one.");

        var user = await userManager.FindByEmailAsync(request.Email);

        // A single generic message for every failure mode, so the endpoint cannot be used to
        // enumerate which e-mail addresses have accounts.
        const string failure = "The e-mail address or password is incorrect.";

        if (user is null)
        {
            logger.LogWarning("Login attempt for unknown account {Email} from {Ip}", request.Email, ClientIp);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: failure);
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login attempt for deactivated account {Email} from {Ip}", request.Email, ClientIp);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: failure);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            // Same status and wording as every other failure: a distinct 423 would
            // confirm the address belongs to a real account.
            logger.LogWarning("Login attempt for locked account {Email} from {Ip}", request.Email, ClientIp);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: failure);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            logger.LogWarning("Failed login for {Email} from {Ip}", request.Email, ClientIp);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: failure);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var result = await IssueTokensAsync(user, ct);
        await audit.LogAsync("Login", nameof(ApplicationUser), user.Id, ct: ct);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var presented = HashRefreshToken(request.RefreshToken);
        var user = await Db.Users.FirstOrDefaultAsync(u => u.RefreshToken == presented, ct);

        if (user is null || !user.IsActive
            || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "The refresh token is invalid or has expired. Please sign in again.");
        }

        return Ok(await IssueTokensAsync(user, ct));
    }

    [HttpGet("me")]
    [AllowPasswordChangePending]
    [Authorize]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        if (user is null) return NotFoundProblem("User");

        var roles = await userManager.GetRolesAsync(user);
        return Ok(ToCurrentUser(user, roles));
    }

    [HttpPost("change-password")]
    [AllowPasswordChangePending]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        if (user is null) return NotFoundProblem("User");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequestProblem(string.Join(" ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = false;
        // Any session issued before the password change is no longer valid.
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await userManager.UpdateAsync(user);

        await audit.LogAsync("ChangePassword", nameof(ApplicationUser), user.Id, ct: ct);
        return NoContent();
    }

    /// <summary>Invalidates the refresh token so the session cannot be resumed.</summary>
    [HttpPost("logout")]
    [AllowPasswordChangePending]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await Db.SaveChangesAsync(ct);
            await audit.LogAsync("Logout", nameof(ApplicationUser), user.Id, ct: ct);
        }

        return NoContent();
    }

    /// <summary>SHA-256 of the token. The value is already 256 bits of randomness
    /// from the CSPRNG, so it needs no salt or work factor - only one-wayness.</summary>
    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task<AuthResultDto> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user.Id, user.Email!, user.FullName, roles);
        var refreshToken = tokens.CreateRefreshToken();

        // Only the hash is kept. A refresh token is a bearer credential valid for
        // days, so storing it verbatim would hand working sessions to anyone who
        // could read the table - a backup, a support query, a SQL injection
        // elsewhere. The client holds the only copy of the token itself.
        user.RefreshToken = HashRefreshToken(refreshToken);
        // The session lives while it is used: each renewal - one every access token's
        // lifetime while the console is in use - moves the end forward, and a session
        // left alone for SessionIdleMinutes cannot be renewed. That is an idle timeout
        // the server enforces, whatever a browser or a copied token does.
        user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.SessionIdleMinutes);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.LastLoginIp = ClientIp;
        await Db.SaveChangesAsync(ct);

        return new AuthResultDto(accessToken, refreshToken, expiresAt, ToCurrentUser(user, roles));
    }

    private static CurrentUserDto ToCurrentUser(ApplicationUser user, IList<string> roles) =>
        new(user.Id, user.Email ?? string.Empty, user.FullName, user.Designation, user.Department,
            user.AvatarUrl, user.MustChangePassword, roles.ToList());
}
