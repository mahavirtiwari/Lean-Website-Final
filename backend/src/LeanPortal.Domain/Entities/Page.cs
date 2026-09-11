using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>A CMS-managed content page (About Scheme, Objective, Privacy Policy, ...).</summary>
public class Page : AuditableEntity, IPublishable
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Optional shorter label used in menus and breadcrumbs.</summary>
    public string? ShortTitle { get; set; }
    public string? Summary { get; set; }
    /// <summary>Sanitised HTML produced by the admin rich-text editor.</summary>
    public string? Body { get; set; }

    public PageTemplate Template { get; set; } = PageTemplate.SidebarLeft;
    /// <summary>Angular route component key when <see cref="Template"/> is Custom.</summary>
    public string? CustomComponent { get; set; }

    public int? ParentId { get; set; }
    public Page? Parent { get; set; }
    public ICollection<Page> Children { get; set; } = [];

    public int SortOrder { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? BannerCaption { get; set; }
    public bool ShowInMainMenu { get; set; }
    public bool ShowSidebarNav { get; set; } = true;

    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public int ViewCount { get; set; }

    public ICollection<PageBlock> Blocks { get; set; } = [];
}
