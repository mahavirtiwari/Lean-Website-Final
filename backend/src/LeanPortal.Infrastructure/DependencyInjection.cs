using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Identity;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services;
using LeanPortal.Infrastructure.Services.Integrations;
using LeanPortal.Infrastructure.Services.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace LeanPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Set it in appsettings, user-secrets, or as an environment variable.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), null);
                sql.CommandTimeout(60);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            });

            if (config.GetValue("Database:EnableDetailedErrors", false))
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging(config.GetValue("Database:EnableSensitiveLogging", false));
            }
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<FileStorageOptions>(config.GetSection(FileStorageOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        // Singleton: the challenges it hands out live in the shared memory cache, so
        // every request has to look them up in the same place.
        services.AddSingleton<ICaptchaService, CaptchaService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISiteSettingsProvider, SiteSettingsProvider>();
        services.AddScoped<IEnquiryRouter, EnquiryRouter>();
        // Keys for the encrypted settings. Persisted to disk so they survive an app
        // pool recycle - without this the mail password would need re-entering after
        // every restart.
        services.AddDataProtection()
            .SetApplicationName("LeanPortal")
            .PersistKeysToFileSystem(new DirectoryInfo(
                config["DataProtection:KeyPath"] is { Length: > 0 } path
                    ? path
                    : Path.Combine(AppContext.BaseDirectory, "keys")));

        services.AddScoped<ISecretProtector, SecretProtector>();
        services.AddScoped<IMailRelayProvider, MailRelayProvider>();

        // Always the real sender now: whether a relay exists is decided per send from
        // the console's settings, which an operator can change without a restart.
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // ------------------------------------------------------ integrations ----
        services.AddOutboundHttp(config);
        services.AddScoped<IZohoDeskClient, ZohoDeskClient>();
        services.AddScoped<IHelpdeskDispatcher, HelpdeskDispatcher>();
        services.AddScoped<IIntegrationCaller, IntegrationCaller>();

        // Singletons so the console's "retry now" and "read documents now" reach the
        // same instances the host is running.
        services.AddSingleton<HelpdeskRetryService>();
        services.AddHostedService(sp => sp.GetRequiredService<HelpdeskRetryService>());
        services.AddSingleton<DocumentTextIndexer>();
        services.AddHostedService(sp => sp.GetRequiredService<DocumentTextIndexer>());

        // One counter for the process, written in batches; see VisitorCounter.
        services.AddSingleton<VisitorCounter>();
        services.AddHostedService(sp => sp.GetRequiredService<VisitorCounter>());

        return services;
    }
}
