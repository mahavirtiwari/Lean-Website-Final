using System.Linq.Expressions;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the LEAN portal CMS (SQL Server).
/// Applies soft-delete filters and stamps audit columns on save.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUser? currentUser = null)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    private readonly ICurrentUser? _currentUser = currentUser;

    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageBlock> PageBlocks => Set<PageBlock>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<DocumentItem> Documents => Set<DocumentItem>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Statistic> Statistics => Set<Statistic>();
    public DbSet<SchemeLevel> SchemeLevels => Set<SchemeLevel>();
    public DbSet<SchemeComponent> SchemeComponents => Set<SchemeComponent>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<GalleryAlbum> GalleryAlbums => Set<GalleryAlbum>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<AwarenessProgramme> AwarenessProgrammes => Set<AwarenessProgramme>();
    public DbSet<Benefit> Benefits => Set<Benefit>();
    public DbSet<Incentive> Incentives => Set<Incentive>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<LoginPortal> LoginPortals => Set<LoginPortal>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<HelpdeskConnection> HelpdeskConnections => Set<HelpdeskConnection>();
    public DbSet<ExternalIntegration> ExternalIntegrations => Set<ExternalIntegration>();
    public DbSet<DocumentText> DocumentTexts => Set<DocumentText>();
    public DbSet<AgencyMailRelay> AgencyMailRelays => Set<AgencyMailRelay>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Shorter Identity table names - the default AspNetX names are kept but schema-qualified.
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            // Global soft-delete filter for every auditable aggregate.
            if (typeof(AuditableEntity).IsAssignableFrom(entity.ClrType))
            {
                var parameter = Expression.Parameter(entity.ClrType, "e");
                var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);
                builder.Entity(entity.ClrType).HasQueryFilter(filter);
            }

            // Never cascade-delete content; blocking deletes surface as validation errors instead.
            foreach (var fk in entity.GetForeignKeys().Where(f => f.DeleteBehavior == DeleteBehavior.Cascade
                                                                  && f.PrincipalEntityType.ClrType != typeof(ApplicationUser)))
            {
                if (fk.DeclaringEntityType.ClrType == typeof(PageBlock) || fk.DeclaringEntityType.ClrType == typeof(GalleryImage))
                    continue; // children die with their parent
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    private void ApplyAuditInformation()
    {
        var user = _currentUser?.UserName ?? "system";
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    break;
            }
        }
    }

    /// <summary>
    /// Adds <paramref name="visits"/> to the visitor counter and returns the new total.
    ///
    /// <para>
    /// A single statement, so two servers writing at once cannot read the same
    /// value and each write back the same increment. The counter is held as a site
    /// setting so an editor can see it, correct it, or seed it with a figure
    /// carried over from a previous site. Called in batches by VisitorCounter.
    /// </para>
    /// </summary>
    public async Task<long> IncrementVisitorCountAsync(long visits = 1, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE SiteSettings
               SET Value = CONVERT(nvarchar(32), TRY_CONVERT(bigint, Value) + @visits)
             OUTPUT INSERTED.Value
             WHERE [Key] = 'stats.visitorCount' AND TRY_CONVERT(bigint, Value) IS NOT NULL;
            """;

        await using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@visits";
        parameter.Value = visits;
        command.Parameters.Add(parameter);

        return await ScalarCountAsync(command, ct);
    }

    /// <summary>The visitor counter's total, read without changing it.</summary>
    public async Task<long> ReadVisitorCountAsync(CancellationToken ct = default)
    {
        await using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT Value FROM SiteSettings WHERE [Key] = 'stats.visitorCount';";
        return await ScalarCountAsync(command, ct);
    }

    private async Task<long> ScalarCountAsync(System.Data.Common.DbCommand command, CancellationToken ct)
    {
        await Database.OpenConnectionAsync(ct);
        try
        {
            var result = await command.ExecuteScalarAsync(ct);
            return long.TryParse(result?.ToString(), out var total) ? total : 0;
        }
        finally
        {
            await Database.CloseConnectionAsync();
        }
    }
}

/// <summary>Ambient information about the caller, used for audit stamping.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
