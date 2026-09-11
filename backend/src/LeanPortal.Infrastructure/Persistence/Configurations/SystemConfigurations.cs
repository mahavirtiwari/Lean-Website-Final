using LeanPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeanPortal.Infrastructure.Persistence.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> b)
    {
        b.ToTable("Partners");
        b.Property(x => x.Name).HasMaxLength(400).IsRequired();
        b.Property(x => x.ShortName).HasMaxLength(100);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.WebsiteUrl).HasMaxLength(500);
        b.Property(x => x.ContactUrl).HasMaxLength(500);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.EnquiryEmail).HasMaxLength(256);
        b.Property(x => x.EnquiryFormUrl).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(60);
        b.Property(x => x.Address).HasMaxLength(600);
        b.Property(x => x.State).HasMaxLength(120);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.RegistrationNumber).HasMaxLength(100);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.Type, x.SortOrder });
    }
}

public class LoginPortalConfiguration : IEntityTypeConfiguration<LoginPortal>
{
    public void Configure(EntityTypeBuilder<LoginPortal> b)
    {
        b.ToTable("LoginPortals");
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Audience).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(600);
        b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.LoginUrl).HasMaxLength(500);
        b.Property(x => x.LoginText).HasMaxLength(80);
        b.Property(x => x.RegisterUrl).HasMaxLength(500);
        b.Property(x => x.RegisterText).HasMaxLength(80);
        b.Property(x => x.AccentColor).HasMaxLength(20);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.ToTable("ContactMessages");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Organisation).HasMaxLength(300);
        b.Property(x => x.UdyamNumber).HasMaxLength(60);
        b.Property(x => x.State).HasMaxLength(120);
        b.Property(x => x.Agency).HasMaxLength(120);
        b.Property(x => x.Subject).HasMaxLength(400).IsRequired();
        b.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Category).HasMaxLength(120);
        b.Property(x => x.AssignedTo).HasMaxLength(256);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.Property(x => x.UserType).HasMaxLength(120);
        b.Property(x => x.IssueType).HasMaxLength(120);
        b.Property(x => x.IssueCategory).HasMaxLength(200);
        b.Property(x => x.IssueSubCategory).HasMaxLength(200);
        b.Property(x => x.HelpdeskTicketId).HasMaxLength(64);
        b.Property(x => x.HelpdeskTicketNumber).HasMaxLength(64);
        b.Property(x => x.HelpdeskLastError).HasMaxLength(1000);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        // The retry sweep asks for exactly this, every couple of minutes.
        b.HasIndex(x => new { x.HelpdeskStatus, x.HelpdeskNextAttemptAt });
    }
}

public class HelpdeskConnectionConfiguration : IEntityTypeConfiguration<HelpdeskConnection>
{
    public void Configure(EntityTypeBuilder<HelpdeskConnection> b)
    {
        b.ToTable("HelpdeskConnections");
        b.Property(x => x.AccountsUrl).HasMaxLength(200).IsRequired();
        b.Property(x => x.ApiBaseUrl).HasMaxLength(200).IsRequired();
        b.Property(x => x.OrganisationId).HasMaxLength(64);
        b.Property(x => x.DepartmentId).HasMaxLength(64);
        b.Property(x => x.ClientId).HasMaxLength(200);
        // Encrypted, so longer than what was typed.
        b.Property(x => x.ClientSecret).HasMaxLength(1000);
        b.Property(x => x.RefreshToken).HasMaxLength(1000);
        b.Property(x => x.Channel).HasMaxLength(60).IsRequired();
        b.Property(x => x.ContactOwnerId).HasMaxLength(64);
        b.Property(x => x.LastCheckResult).HasMaxLength(1000);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ExternalIntegrationConfiguration : IEntityTypeConfiguration<ExternalIntegration>
{
    public void Configure(EntityTypeBuilder<ExternalIntegration> b)
    {
        b.ToTable("ExternalIntegrations");
        b.Property(x => x.Key).HasMaxLength(60).IsRequired();
        b.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Intro).HasMaxLength(2000);
        b.Property(x => x.Url).HasMaxLength(1000);
        b.Property(x => x.ApiUrl).HasMaxLength(1000);
        b.Property(x => x.ApiMethod).HasMaxLength(10).IsRequired();
        b.Property(x => x.ApiHeaderName).HasMaxLength(100);
        b.Property(x => x.ApiHeaderValue).HasMaxLength(2000);
        b.Property(x => x.ApiResultPath).HasMaxLength(200);
        b.Property(x => x.ApiTotalPath).HasMaxLength(200);
        b.Property(x => x.InputLabel).HasMaxLength(120);
        b.Property(x => x.LastCheckResult).HasMaxLength(1000);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Key).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class AgencyMailRelayConfiguration : IEntityTypeConfiguration<AgencyMailRelay>
{
    public void Configure(EntityTypeBuilder<AgencyMailRelay> b)
    {
        b.ToTable("AgencyMailRelays");
        b.Property(x => x.Host).HasMaxLength(200);
        b.Property(x => x.Encryption).HasMaxLength(20).IsRequired();
        b.Property(x => x.Username).HasMaxLength(256);
        // Encrypted, so longer than what was typed.
        b.Property(x => x.Password).HasMaxLength(1000);
        b.Property(x => x.FromAddress).HasMaxLength(256);
        b.Property(x => x.FromName).HasMaxLength(200);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.PartnerId).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentTextConfiguration : IEntityTypeConfiguration<DocumentText>
{
    public void Configure(EntityTypeBuilder<DocumentText> b)
    {
        b.ToTable("DocumentTexts");
        b.HasKey(x => x.DocumentId);
        b.Property(x => x.DocumentId).ValueGeneratedNever();
        b.Property(x => x.SourceUrl).HasMaxLength(500).IsRequired();
        b.HasOne<DocumentItem>().WithOne().HasForeignKey<DocumentText>(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> b)
    {
        b.ToTable("Subscribers");
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.ConfirmationToken).HasMaxLength(200);
        b.Property(x => x.Source).HasMaxLength(120);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.ToTable("SiteSettings");
        b.Property(x => x.Key).HasMaxLength(150).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(250);
        b.Property(x => x.Description).HasMaxLength(600);
        b.Property(x => x.Group).HasMaxLength(100).IsRequired();
        b.Property(x => x.Section).HasMaxLength(100);
        b.Property(x => x.DataType).HasMaxLength(40).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Key).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.ToTable("MediaAssets");
        b.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        b.Property(x => x.StoredFileName).HasMaxLength(300).IsRequired();
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        b.Property(x => x.AltText).HasMaxLength(300);
        b.Property(x => x.Caption).HasMaxLength(500);
        b.Property(x => x.Folder).HasMaxLength(120);
        b.Property(x => x.Checksum).HasMaxLength(80);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.Folder, x.CreatedAt });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.Property(x => x.UserId).HasMaxLength(450);
        b.Property(x => x.UserName).HasMaxLength(256);
        b.Property(x => x.Action).HasMaxLength(60).IsRequired();
        b.Property(x => x.EntityName).HasMaxLength(150).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(100);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.HasIndex(x => new { x.EntityName, x.EntityId });
        b.HasIndex(x => x.Timestamp);
    }
}
