using System.Text;
using LeanPortal.Application.Contracts;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Public;

/// <summary>
/// The parts of the portal whose content comes from elsewhere: certificate
/// verification, the list of certified units, and - in its API mode - the
/// assistant.
/// </summary>
[Route("site/integrations")]
[EnableRateLimiting("public")]
public class IntegrationsController(
    ApplicationDbContext db,
    IIntegrationCaller caller,
    IWebHostEnvironment environment) : ApiControllerBase(db)
{
    /// <summary>Only these are offered to the public; anything else is not found.</summary>
    private static readonly HashSet<string> Keys = ["certificate-verification", "certified-units", "assistant"];

    /// <summary>How the page should present the service.</summary>
    [HttpGet("{key}")]
    [OutputCache(PolicyName = "public-content")]
    [ProducesResponseType<PublicIntegrationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicIntegrationDto>> Get(string key, CancellationToken ct)
    {
        var integration = await FindAsync(key, ct);
        if (integration is null) return NotFound();

        return Ok(new PublicIntegrationDto(
            integration.Key,
            integration.Mode,
            integration.Title,
            integration.Intro,
            integration.Mode is IntegrationMode.Link or IntegrationMode.Frame ? SafeUrl(integration.Url) : null,
            Math.Clamp(integration.FrameHeight, 240, 2400),
            integration.InputLabel,
            IntegrationCaller.ParseMappings(integration.ApiFieldMap).Select(m => m.Label).ToList()));
    }

    /// <summary>
    /// The provider's embed code, as a page of its own for a sandboxed frame.
    ///
    /// Never put into a portal page. The console's session token lives in the
    /// portal's origin, and a snippet running there could read it; framed like this,
    /// without allow-same-origin, it runs in an origin of its own and can reach
    /// nothing of ours. That isolation is also why this page may carry a relaxed
    /// security policy of its own - the snippet's scripts and styles come from
    /// wherever the provider serves them.
    /// </summary>
    [HttpGet("{key}/frame")]
    // Not cached: a cached copy would be served with the X-Frame-Options header the
    // security middleware adds ahead of the cache, which this response removes.
    [OutputCache(NoStore = true)]
    public async Task<IActionResult> Frame(string key, CancellationToken ct)
    {
        var integration = await FindAsync(key, ct);
        if (integration is null || integration.Mode != IntegrationMode.Embed || string.IsNullOrWhiteSpace(integration.EmbedCode))
            return NotFound();

        var ancestors = environment.IsDevelopment() ? "'self' http://localhost:4200" : "'self'";

        Response.Headers.ContentSecurityPolicy =
            "default-src https: data: blob:; " +
            "script-src https: 'unsafe-inline' 'unsafe-eval'; " +
            "style-src https: 'unsafe-inline'; " +
            "img-src https: data: blob:; " +
            "connect-src https: wss:; " +
            "frame-src https:; " +
            "form-action https:; " +
            "base-uri 'none'; " +
            $"frame-ancestors {ancestors}";

        // frame-ancestors decides who may frame this; X-Frame-Options would block the
        // development front end, which runs on another port.
        Response.Headers.Remove("X-Frame-Options");
        Response.Headers.CacheControl = "no-cache";

        var html = new StringBuilder()
            .Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<meta name=\"referrer\" content=\"strict-origin-when-cross-origin\">")
            .Append("<title>").Append(System.Net.WebUtility.HtmlEncode(integration.Title ?? "Service")).Append("</title>")
            .Append("<style>html,body{margin:0;padding:0;font-family:system-ui,sans-serif;background:#fff}</style>")
            .Append("</head><body>")
            .Append(integration.EmbedCode)
            .Append("</body></html>")
            .ToString();

        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>Checks a certificate through the provider's API.</summary>
    [HttpGet("certificate-verification/lookup")]
    [EnableRateLimiting("lookup")]
    [OutputCache(NoStore = true)]
    [ProducesResponseType<IntegrationLookupDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationLookupDto>> Verify([FromQuery] string? number, CancellationToken ct)
    {
        var integration = await FindAsync("certificate-verification", ct);
        if (integration is null || integration.Mode != IntegrationMode.Api) return NotFound();

        var value = number?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 100)
            return BadRequestProblem("Enter the certificate number as it is printed on the certificate.");

        var result = await caller.CallAsync(integration, new Dictionary<string, string?>
        {
            ["certificateNumber"] = value,
            ["query"] = value,
        }, ct);

        if (!result.Ok)
            return Ok(new IntegrationLookupDto(false,
                "The verification service could not be reached. Please try again later.", [], 0));

        var rows = result.Rows.Take(1).Select(ToRow).ToList();
        return Ok(new IntegrationLookupDto(rows.Count > 0,
            rows.Count > 0 ? null : "No certificate was found with that number.", rows, rows.Count));
    }

    /// <summary>A page of certified units from the provider's API.</summary>
    [HttpGet("certified-units/list")]
    [OutputCache(NoStore = true)]
    [ProducesResponseType<IntegrationLookupDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationLookupDto>> Units(
        [FromQuery] string? search, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var integration = await FindAsync("certified-units", ct);
        if (integration is null || integration.Mode != IntegrationMode.Api) return NotFound();

        page = Math.Clamp(page, 1, 1000);
        const int pageSize = 20;

        var result = await caller.CallAsync(integration, new Dictionary<string, string?>
        {
            ["search"] = search?.Trim() is { Length: <= 100 } s ? s : null,
            ["query"] = search?.Trim() is { Length: <= 100 } q ? q : null,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["offset"] = ((page - 1) * pageSize).ToString(),
        }, ct);

        if (!result.Ok)
            return Ok(new IntegrationLookupDto(false,
                "The list of certified units could not be loaded. Please try again later.", [], 0));

        var rows = result.Rows.Take(200).Select(ToRow).ToList();
        return Ok(new IntegrationLookupDto(rows.Count > 0, rows.Count > 0 ? null : "No certified units match.",
            rows, result.Total ?? rows.Count));
    }

    private static IntegrationRowDto ToRow(IReadOnlyList<KeyValuePair<string, string?>> values) =>
        new(values.Select(v => new IntegrationValueDto(v.Key, v.Value)).ToList());

    private async Task<ExternalIntegration?> FindAsync(string key, CancellationToken ct) =>
        Keys.Contains(key)
            ? await Db.ExternalIntegrations.AsNoTracking().FirstOrDefaultAsync(i => i.Key == key, ct)
            : null;

    /// <summary>
    /// Only https goes to the page. The address ends up in a link or a frame, and an
    /// unchecked one - javascript:, or plain http - is a way to put anything at all
    /// on a government page.
    /// </summary>
    public static string? SafeUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.ToString()
            : null;
}
