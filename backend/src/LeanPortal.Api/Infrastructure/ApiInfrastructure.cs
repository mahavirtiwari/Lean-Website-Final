using System.Globalization;
using System.Threading.RateLimiting;
using Ganss.Xss;
using LeanPortal.Application.Interfaces;
using LeanPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace LeanPortal.Api.Infrastructure;

/// <summary>Authorisation policy names used by the admin controllers.</summary>
public static class Policies
{
    public const string CanEdit = "CanEdit";
    public const string CanPublish = "CanPublish";
    public const string CanAdminister = "CanAdminister";
}

/// <summary>
/// Removes scripting and unsafe markup from HTML submitted through the admin rich-text editor,
/// so a compromised editor account cannot inject script into the public site.
/// </summary>
public class HtmlContentSanitizer : IContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // Markup the editors legitimately need.
        foreach (var tag in new[]
                 {
                     "h2", "h3", "h4", "h5", "p", "ul", "ol", "li", "strong", "em", "u", "br", "hr",
                     "blockquote", "a", "img", "figure", "figcaption", "table", "thead", "tbody", "tfoot",
                     "tr", "th", "td", "caption", "div", "span", "sup", "sub", "small", "code", "pre",
                     "section", "article", "abbr", "dl", "dt", "dd", "iframe"
                 })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        foreach (var attribute in new[]
                 {
                     "href", "src", "alt", "title", "class", "id", "target", "rel", "colspan", "rowspan",
                     "scope", "width", "height", "loading", "type", "start", "allow", "allowfullscreen",
                     "frameborder", "aria-label", "aria-labelledby", "aria-describedby", "role", "lang", "dir"
                 })
        {
            _sanitizer.AllowedAttributes.Add(attribute);
        }

        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedSchemes.Add("tel");

        // Only embeds from trusted government / video hosts survive sanitisation.
        _sanitizer.AllowedAtRules.Clear();
        _sanitizer.RemovingTag += (_, e) =>
        {
            if (!e.Tag.NodeName.Equals("IFRAME", StringComparison.OrdinalIgnoreCase)) return;

            var src = e.Tag.GetAttribute("src") ?? string.Empty;
            e.Cancel = TrustedEmbedHosts.Any(host =>
                src.StartsWith($"https://{host}", StringComparison.OrdinalIgnoreCase));
        };
    }

    private static readonly string[] TrustedEmbedHosts =
    [
        "www.youtube.com/embed/",
        "youtube.com/embed/",
        "www.youtube-nocookie.com/embed/",
        "player.vimeo.com/video/",
        "www.google.com/maps/embed"
    ];

    public string? Sanitize(string? html) =>
        string.IsNullOrWhiteSpace(html) ? html : _sanitizer.Sanitize(html);
}

/// <summary>
/// Drops the cached public responses as soon as an editor changes anything.
///
/// Public content is served from the output cache for a few minutes, which is what keeps the
/// front page cheap under load - but it also means an editor would save a change and then not see
/// it. Every successful write through the admin API therefore evicts the whole public tag: edits
/// are rare next to reads, so throwing the lot away is cheaper than tracking which endpoints a
/// given record feeds.
/// </summary>
public class EvictPublicCacheFilter(IOutputCacheStore store) : IAsyncActionFilter
{
    /// <summary>Tag carried by every cached public response; see the output cache policy.</summary>
    public const string PublicTag = "public-content";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (HttpMethods.IsGet(context.HttpContext.Request.Method) ||
            HttpMethods.IsHead(context.HttpContext.Request.Method))
        {
            return;
        }

        // The /api prefix is the path base, not part of the path: IIS strips it for the
        // /api application, and UsePathBase does the same in development. This once
        // checked for "/api/admin", which stopped matching when the prefix moved into
        // the path base - and from then on no save in the console cleared the public
        // cache, so every change took up to five minutes to show. Both are accepted
        // so the filter holds however the API is hosted.
        var path = context.HttpContext.Request.Path;
        if (!path.StartsWithSegments("/admin") && !path.StartsWithSegments("/api/admin")) return;

        var status = executed.HttpContext.Response.StatusCode;
        if (status is < 200 or >= 300) return;

        await store.EvictByTagAsync(PublicTag, context.HttpContext.RequestAborted);
    }
}

/// <summary>
/// Turns unhandled exceptions into RFC 7807 problem details, so the Angular client always gets a
/// predictable shape and internal details never leak to the browser.
/// </summary>
public class ApiExceptionFilter(IHostEnvironment env, ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;

        var (status, title) = context.Exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "You do not have access to this resource."),
            ArgumentException or InvalidOperationException when context.Exception.Data.Contains("client")
                => (StatusCodes.Status400BadRequest, context.Exception.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                "The record was changed by another user. Reload and try again."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= 500)
            logger.LogError(context.Exception, "Unhandled exception. TraceId {TraceId}", traceId);
        else
            logger.LogWarning(context.Exception, "Request failed with {Status}. TraceId {TraceId}", status, traceId);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.HttpContext.Request.Path,
            Detail = env.IsDevelopment() ? context.Exception.ToString() : null
        };
        problem.Extensions["traceId"] = traceId;

        context.Result = new ObjectResult(problem) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}

public static class ApiServiceExtensions
{
    /// <summary>
    /// Throttles the endpoints an anonymous visitor can reach, so the public form endpoints cannot
    /// be used to flood the enquiry inbox and the login endpoint resists credential stuffing.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        // Per client address. A page view makes several API calls, and a ministry
        // office or a campus reaches the site through one address, so the public
        // allowance is generous; it is there to stop one machine hammering the site,
        // not to ration ordinary reading.
        var publicPerMinute = config.GetValue("RateLimiting:PublicPerMinute", 600);
        var formsPerMinute = config.GetValue("RateLimiting:FormsPerMinute", 5);
        var loginPerMinute = config.GetValue("RateLimiting:LoginPerMinute", 8);
        var captchaPerMinute = config.GetValue("RateLimiting:CaptchaPerMinute", 30);
        // Looking a certificate up sends nothing and changes nothing; it only costs
        // one call to the provider. The form allowance would stop an officer on the
        // sixth certificate of the morning, and everyone else in that office with him.
        var lookupPerMinute = config.GetValue("RateLimiting:LookupPerMinute", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = publicPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));

            options.AddPolicy("forms", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = formsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));

            // Drawing a challenge is cheap and a person may want several; this only
            // stops a script harvesting them.
            options.AddPolicy("captcha", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = captchaPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));

            // A read-only query put to a provider on the visitor's behalf.
            options.AddPolicy("lookup", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = lookupPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));

            options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });

        return services;
    }

    private static string ClientKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public static class ApiApplicationExtensions
{
    /// <summary>Adds the response headers expected of an Indian Government (GIGW) portal.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers.Remove("X-Powered-By");
            headers.Remove("Server");
            await next();
        });

    /// <summary>
    /// Serves the uploads folder read-only with an explicit content-type map, so an uploaded file
    /// can never be served as an executable or script type.
    /// </summary>
    public static IApplicationBuilder UseUploadedFiles(this IApplicationBuilder app, IConfiguration config)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var options = config.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
                      ?? new FileStorageOptions();

        var root = options.RootPath is { Length: > 0 } custom
            ? custom
            : Path.Combine(env.ContentRootPath, "wwwroot", "uploads");

        Directory.CreateDirectory(root);

        var provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".csv"] = "text/csv",
            [".zip"] = "application/zip"
        });

        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = options.RequestPath.TrimEnd('/'),
            ContentTypeProvider = provider,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = ctx =>
            {
                var headers = ctx.Context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Content-Disposition"] = "inline";

                // Uploads are editor-supplied and are served from the portal's own
                // origin, so they are locked down as data rather than as documents:
                // nothing may load, nothing may run, and the sandbox denies scripts
                // even if a script-capable type ever reaches this folder. Belt and
                // braces alongside the upload allow-list.
                headers["Content-Security-Policy"] = "default-src 'none'; sandbox";

                headers["Cache-Control"] =
                    "public,max-age=" + TimeSpan.FromDays(7).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
        });
    }
}
