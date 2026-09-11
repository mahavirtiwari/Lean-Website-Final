using System.Text;
using System.Text.Json.Serialization;
using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Identity;
using LeanPortal.Infrastructure;
using LeanPortal.Infrastructure.Persistence.Seed;
using LeanPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------ logging ----
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --------------------------------------------------------------- application ----
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IContentSanitizer, HtmlContentSanitizer>();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ApiExceptionFilter>();
        options.Filters.Add<EvictPublicCacheFilter>();
        // Before anything else reads a request: an account that has not changed its
        // temporary password must not reach the rest of the API.
        options.Filters.Add<MustChangePasswordFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddOutputCache(options =>
{
    // Public content is cached briefly; the admin API opts out with [OutputCache(NoStore = true)].
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromSeconds(60)));
    // Tagged so an admin write can drop every cached public response at once;
    // see EvictPublicCacheFilter.
    options.AddPolicy("public-content", policy =>
        policy.Expire(TimeSpan.FromMinutes(5)).Tag(EvictPublicCacheFilter.PublicTag));
});

// ----------------------------------------------------------- authentication ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        // Development convenience only. Production start-up fails fast instead.
        jwt.Key = "dev-only-signing-key-please-replace-in-every-real-environment";
        builder.Configuration[$"{JwtOptions.SectionName}:Key"] = jwt.Key;
    }
    else
    {
        throw new InvalidOperationException(
            "Jwt:Key must be configured with at least 32 bytes of entropy outside Development. " +
            "Set it via an environment variable or the Windows Server machine configuration.");
    }
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanEdit, p => p.RequireRole(
        Roles.SuperAdmin, Roles.Administrator, Roles.Editor, Roles.Publisher));
    options.AddPolicy(Policies.CanPublish, p => p.RequireRole(
        Roles.SuperAdmin, Roles.Administrator, Roles.Publisher));
    options.AddPolicy(Policies.CanAdminister, p => p.RequireRole(
        Roles.SuperAdmin, Roles.Administrator));
});

// ---------------------------------------------------------------------- cors ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:4200"];

builder.Services.AddCors(options => options.AddPolicy("AngularApp", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ------------------------------------------------------------------- swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MSME Competitive (LEAN) Scheme Portal API",
        Version = "v1",
        Description = "Public content API and administrative CMS API for the LEAN Scheme portal."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /api/auth/login."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// ------------------------------------------------------------ rate limiting ----
builder.Services.AddRateLimiting(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------- pipeline ----
app.UseSerilogRequestLogging();

// Behind IIS / ARR the real client IP and scheme arrive in forwarded headers.
// Trusted only from loopback, unless a load balancer or WAF in front of the
// server is named in ForwardedHeaders:KnownProxies - then from it too. Without
// that, every visitor behind it shares its address, and one rate-limit allowance.
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
{
    if (System.Net.IPAddress.TryParse(proxy, out var address)) forwarded.KnownProxies.Add(address);
}
app.UseForwardedHeaders(forwarded);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "LEAN Portal API v1"));
}

// HSTS is not sent from here. TLS terminates at IIS, and the site's web.config
// sends the header for the whole site, this application included. Sending it
// from both places would put two Strict-Transport-Security headers on every API
// response, and a browser that sees more than one honours only the first - so
// the two would have to be kept in step forever to mean anything. One place,
// next to the certificate, is the arrangement that holds.
//
// The redirect to HTTPS is IIS's job too, for the same reason: it can answer
// before the request reaches this process. This stays because it costs nothing
// and covers the case of the API being hosted on its own.
app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseResponseCompression();

// Uploads first. They live under wwwroot, so the default static-file middleware
// would otherwise serve them on its own terms and the hardening below - the
// restricted content-type map and the sandbox headers - would never run.
// The API is deployed as an IIS application under /api, which strips that prefix
// before routing - so the controllers do not repeat it. In development the API is
// its own root, and this puts the prefix back so both environments answer on the
// same addresses. Under IIS the path has already been stripped, so this matches
// nothing and does nothing.
app.UsePathBase("/api");

app.UseUploadedFiles(builder.Configuration);
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AngularApp");
app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }))
   .AllowAnonymous()
   .ExcludeFromDescription();

// ------------------------------------------------------------------- startup ----
if (app.Configuration.GetValue("Database:MigrateAndSeedOnStartup", true))
{
    await DataSeeder.SeedAsync(app.Services);
}

app.Run();

/// <summary>Exposed so the integration test host can reference the entry point assembly.</summary>
public partial class Program;
