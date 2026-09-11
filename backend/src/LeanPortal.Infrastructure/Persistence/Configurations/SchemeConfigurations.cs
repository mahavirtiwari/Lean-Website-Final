using LeanPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeanPortal.Infrastructure.Persistence.Configurations;

public class StatisticConfiguration : IEntityTypeConfiguration<Statistic>
{
    public void Configure(EntityTypeBuilder<Statistic> b)
    {
        b.ToTable("Statistics");
        b.Property(x => x.Label).HasMaxLength(200).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(20);
        b.Property(x => x.Suffix).HasMaxLength(20);
        b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.LinkUrl).HasMaxLength(500);
        b.Property(x => x.SourceQueryKey).HasMaxLength(100);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public class BenefitConfiguration : IEntityTypeConfiguration<Benefit>
{
    public void Configure(EntityTypeBuilder<Benefit> b)
    {
        b.ToTable("Benefits");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(300);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.LinkUrl).HasMaxLength(500);
        b.Property(x => x.LinkText).HasMaxLength(60);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);

        // Defaulted true in the database as well as in the entity: without it the
        // migration adds the column as false and every existing row is hidden the
        // moment it ships, which is exactly what happened to the gallery.
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public class IncentiveConfiguration : IEntityTypeConfiguration<Incentive>
{
    public void Configure(EntityTypeBuilder<Incentive> b)
    {
        b.ToTable("Incentives");
        b.Property(x => x.Title).HasMaxLength(400).IsRequired();
        b.Property(x => x.Level).HasMaxLength(80);
        b.Property(x => x.State).HasMaxLength(120);
        b.Property(x => x.IssuerName).HasMaxLength(300);
        b.Property(x => x.IssuerLogoUrl).HasMaxLength(500);
        b.Property(x => x.ContactName).HasMaxLength(300);
        b.Property(x => x.ContactEmail).HasMaxLength(256);
        b.Property(x => x.ContactPhone).HasMaxLength(120);
        b.Property(x => x.Document1Url).HasMaxLength(500);
        b.Property(x => x.Document1Label).HasMaxLength(120);
        b.Property(x => x.Document2Url).HasMaxLength(500);
        b.Property(x => x.Document2Label).HasMaxLength(120);
        b.Property(x => x.VideoUrl).HasMaxLength(500);
        b.Property(x => x.AvailUrl).HasMaxLength(500);
        b.Property(x => x.AvailLabel).HasMaxLength(60);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);

        b.Property(x => x.IsActive).HasDefaultValue(true);

        // The public list is always filtered by category first, then ordered.
        b.HasIndex(x => new { x.Category, x.IsActive, x.SortOrder });
        b.HasIndex(x => x.State);
    }
}

public class SchemeLevelConfiguration : IEntityTypeConfiguration<SchemeLevel>
{
    public void Configure(EntityTypeBuilder<SchemeLevel> b)
    {
        b.ToTable("SchemeLevels");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.BadgeLabel).HasMaxLength(60);
        b.Property(x => x.Tagline).HasMaxLength(300);
        b.Property(x => x.FeeStructure).HasMaxLength(500);
        b.Property(x => x.Duration).HasMaxLength(100);
        b.Property(x => x.IconUrl).HasMaxLength(500);
        b.Property(x => x.CertificateImageUrl).HasMaxLength(500);
        b.Property(x => x.AccentColor).HasMaxLength(20);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.SortOrder);
    }
}

public class SchemeComponentConfiguration : IEntityTypeConfiguration<SchemeComponent>
{
    public void Configure(EntityTypeBuilder<SchemeComponent> b)
    {
        b.ToTable("SchemeComponents");
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.LinkUrl).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.SortOrder);
    }
}

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> b)
    {
        b.ToTable("Faqs");
        b.Property(x => x.Question).HasMaxLength(600).IsRequired();
        b.Property(x => x.Category).HasMaxLength(120);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.Category, x.SortOrder });
    }
}

public class GalleryAlbumConfiguration : IEntityTypeConfiguration<GalleryAlbum>
{
    public void Configure(EntityTypeBuilder<GalleryAlbum> b)
    {
        b.ToTable("GalleryAlbums");
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.CoverImageUrl).HasMaxLength(500);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasMany(x => x.Images).WithOne(x => x.Album)
            .HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GalleryImageConfiguration : IEntityTypeConfiguration<GalleryImage>
{
    public void Configure(EntityTypeBuilder<GalleryImage> b)
    {
        b.ToTable("GalleryImages");
        b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.VideoUrl).HasMaxLength(500);
        // Defaulted in the database as well as the entity, so the column backfills to
        // visible: every photograph already in an album was being shown.
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.Caption).HasMaxLength(400);
        b.Property(x => x.AltText).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.AlbumId, x.SortOrder });
    }
}

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> b)
    {
        b.ToTable("Testimonials");
        b.Property(x => x.UnitName).HasMaxLength(300).IsRequired();
        b.Property(x => x.PersonName).HasMaxLength(200);
        b.Property(x => x.Designation).HasMaxLength(200);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.Sector).HasMaxLength(200);
        b.Property(x => x.Quote).HasMaxLength(2000).IsRequired();
        b.Property(x => x.PhotoUrl).HasMaxLength(500);
        b.Property(x => x.VideoUrl).HasMaxLength(500);
        b.Property(x => x.ImpactHighlight).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.Status, x.SortOrder });
    }
}

public class AwarenessProgrammeConfiguration : IEntityTypeConfiguration<AwarenessProgramme>
{
    public void Configure(EntityTypeBuilder<AwarenessProgramme> b)
    {
        b.ToTable("AwarenessProgrammes");
        b.Property(x => x.ProgrammeCode).HasMaxLength(60).IsRequired();
        b.Property(x => x.Title).HasMaxLength(400).IsRequired();
        b.Property(x => x.ProgrammeType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Agency).HasMaxLength(100);
        b.Property(x => x.State).HasMaxLength(120);
        b.Property(x => x.District).HasMaxLength(120);
        b.Property(x => x.Venue).HasMaxLength(500);
        b.Property(x => x.RegistrationUrl).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.ProgrammeCode).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.State, x.StartDate });
        b.HasIndex(x => new { x.ProgrammeType, x.ProgrammeStatus });
    }
}
