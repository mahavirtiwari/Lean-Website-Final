using LeanPortal.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies migrations and populates the CMS with the initial LEAN scheme content set.
/// Every seed step is idempotent: it only inserts rows that are not already present, so it is
/// safe to run on every application start and after future migrations.
/// </summary>
public static partial class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(ct);

        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminUserAsync(userManager, config, sp.GetRequiredService<IHostEnvironment>(), logger);

        await SeedSettingsAsync(db, logger, ct);
        await SeedSchemeAsync(db, logger, ct);
        await SeedPagesAsync(db, logger, ct);
        await SeedMenusAsync(db, logger, ct);
        await SeedContentAsync(db, logger, ct);
        await SeedHomeBlocksAsync(db, logger, ct);

        // Last, and a top-up rather than a first-run seed: the pages GIGW requires
        // have to reach installations that were seeded before those pages existed.
        await RenameSchemeLevelsAsync(db, logger, ct);
        await SeedBenefitsAsync(db, logger, ct);
        await SeedGigwComplianceAsync(db, logger, ct);
        await SeedFooterBarAsync(db, logger, ct);
        await SeedIntegrationsAsync(db, logger, ct);

        logger.LogInformation("Seeding complete.");
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var descriptions = new Dictionary<string, string>
        {
            [Roles.SuperAdmin] = "Full control including user management and system settings.",
            [Roles.Administrator] = "Manages all content, users and settings.",
            [Roles.Editor] = "Creates and edits content but cannot publish.",
            [Roles.Publisher] = "Reviews and publishes content submitted by editors.",
            [Roles.Viewer] = "Read-only access to the admin console and reports."
        };

        foreach (var role in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            await roleManager.CreateAsync(new ApplicationRole(role) { Description = descriptions[role] });
            logger.LogInformation("Created role {Role}", role);
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager, IConfiguration config, IHostEnvironment environment, ILogger logger)
    {
        var email = config["Seed:AdminEmail"] ?? "admin@lean.msme.gov.in";
        if (await userManager.FindByEmailAsync(email) is not null) return;

        // Supplied via user-secrets / environment in every environment except local development.
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            // The fallback below is published in this repository, so on a server anyone
            // could sign in with it before the operator did - and the forced password
            // change would then be made by them. Outside a developer's machine the
            // account is not created at all until a password is configured.
            if (!environment.IsDevelopment())
            {
                logger.LogError(
                    "No administrator was created: Seed:AdminPassword is not configured. Set it (an environment " +
                    "variable Seed__AdminPassword, or appsettings.Production.json) and restart the site; the " +
                    "account is created with that password and must change it at first sign-in.");
                return;
            }

            password = "ChangeMe@Lean2026";
            logger.LogWarning(
                "Seed:AdminPassword is not configured - creating {Email} with the default development password. " +
                "Set Seed:AdminPassword before deploying, and change it immediately after first sign-in.", email);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Portal Administrator",
            Designation = "System Administrator",
            Department = "Ministry of MSME",
            IsActive = true,
            MustChangePassword = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create seed administrator: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        logger.LogInformation("Created seed administrator {Email}", email);
    }
}
