using LeanPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeanPortal.Infrastructure.Persistence.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> b)
    {
        b.ToTable("Pages");
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.ShortTitle).HasMaxLength(150);
        b.Property(x => x.Summary).HasMaxLength(1000);
        b.Property(x => x.CustomComponent).HasMaxLength(100);
        b.Property(x => x.BannerImageUrl).HasMaxLength(500);
        b.Property(x => x.BannerCaption).HasMaxLength(300);
        b.Property(x => x.MetaTitle).HasMaxLength(300);
        b.Property(x => x.MetaDescription).HasMaxLength(500);
        b.Property(x => x.MetaKeywords).HasMaxLength(500);
        b.Property(x => x.OgImageUrl).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);

        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Status, x.SortOrder });

        b.HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Blocks).WithOne(x => x.Page)
            .HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PageBlockConfiguration : IEntityTypeConfiguration<PageBlock>
{
    public void Configure(EntityTypeBuilder<PageBlock> b)
    {
        b.ToTable("PageBlocks");
        b.Property(x => x.Eyebrow).HasMaxLength(200);
        b.Property(x => x.Heading).HasMaxLength(300);
        b.Property(x => x.SubHeading).HasMaxLength(500);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.VideoUrl).HasMaxLength(500);
        b.Property(x => x.PrimaryLinkText).HasMaxLength(120);
        b.Property(x => x.PrimaryLinkUrl).HasMaxLength(500);
        b.Property(x => x.SecondaryLinkText).HasMaxLength(120);
        b.Property(x => x.SecondaryLinkUrl).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.PageId, x.SortOrder });
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.ToTable("MenuItems");
        b.Property(x => x.Label).HasMaxLength(150).IsRequired();
        b.Property(x => x.Url).HasMaxLength(500);
        b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.Location, x.SortOrder });
        b.HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Page).WithMany()
            .HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("Posts");
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.Title).HasMaxLength(400).IsRequired();
        b.Property(x => x.Excerpt).HasMaxLength(1000);
        b.Property(x => x.CoverImageUrl).HasMaxLength(500);
        b.Property(x => x.Author).HasMaxLength(200);
        b.Property(x => x.AttachmentUrl).HasMaxLength(500);
        b.Property(x => x.AttachmentLabel).HasMaxLength(200);
        b.Property(x => x.MetaDescription).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Type, x.Status, x.PublishedAt });
    }
}

public class DocumentItemConfiguration : IEntityTypeConfiguration<DocumentItem>
{
    public void Configure(EntityTypeBuilder<DocumentItem> b)
    {
        b.ToTable("Documents");
        b.Property(x => x.Title).HasMaxLength(400).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.FileType).HasMaxLength(20).IsRequired();
        b.Property(x => x.Language).HasMaxLength(50);
        b.Property(x => x.Version).HasMaxLength(50);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.Category, x.SortOrder });
    }
}

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> b)
    {
        b.ToTable("Banners");
        b.Property(x => x.Eyebrow).HasMaxLength(200);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.HighlightedTitle).HasMaxLength(300);
        b.Property(x => x.Subtitle).HasMaxLength(800);
        b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.MobileImageUrl).HasMaxLength(500);
        b.Property(x => x.AltText).HasMaxLength(300);
        b.Property(x => x.PrimaryButtonText).HasMaxLength(120);
        b.Property(x => x.PrimaryButtonUrl).HasMaxLength(500);
        b.Property(x => x.SecondaryButtonText).HasMaxLength(120);
        b.Property(x => x.SecondaryButtonUrl).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
