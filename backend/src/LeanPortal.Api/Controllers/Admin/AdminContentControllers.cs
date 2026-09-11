using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Common;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Admin;

// -------------------------------------------------------------------- posts ----

[Route("admin/posts")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public class AdminPostsController(
    ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminPostListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminPostListItemDto>>> GetAll(
        [FromQuery] PagedQuery query, [FromQuery] PostType? type, [FromQuery] PublishStatus? status,
        CancellationToken ct)
    {
        var source = Db.Posts.AsNoTracking().AsQueryable();

        if (type is not null) source = source.Where(p => p.Type == type);
        if (status is not null) source = source.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                                       || EF.Functions.Like(p.Slug, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<AdminPostListItemDto>.Create(
            items.Select(MappingExtensions.ToListItemDto).ToList(), query.Page, query.PageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetById(int id, CancellationToken ct)
    {
        var post = await Db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return post is null ? NotFoundProblem("Post") : Ok(post.ToDto());
    }

    [HttpPost]
    [ProducesResponseType<PostDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostDto>> Create([FromBody] SavePostRequest request, CancellationToken ct)
    {
        if (await SlugTakenAsync(request.Slug, null, ct))
            return BadRequestProblem($"The slug '{request.Slug}' is already in use.");

        var post = new Post { Status = PublishStatus.Draft };
        Apply(request, post);

        Db.Posts.Add(post);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Create", nameof(Post), post.Id.ToString(), new { post.Slug, post.Title }, ct);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<PostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> Update(int id, [FromBody] SavePostRequest request, CancellationToken ct)
    {
        var post = await Db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return NotFoundProblem("Post");

        if (await SlugTakenAsync(request.Slug, id, ct))
            return BadRequestProblem($"The slug '{request.Slug}' is already in use.");

        Apply(request, post);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Update", nameof(Post), id.ToString(), new { post.Slug, post.Title }, ct);
        return Ok(post.ToDto());
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var post = await Db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return NotFoundProblem("Post");

        post.Status = request.Status;
        if (request.Status == PublishStatus.Published)
            post.PublishedAt = request.PublishedAt ?? post.PublishedAt ?? DateTimeOffset.UtcNow;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("ChangeStatus", nameof(Post), id.ToString(), new { request.Status }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var post = await Db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return NotFoundProblem("Post");

        post.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(Post), id.ToString(), ct: ct);
        return NoContent();
    }

    private Task<bool> SlugTakenAsync(string slug, int? excludeId, CancellationToken ct) =>
        Db.Posts.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), ct);

    private void Apply(SavePostRequest r, Post p)
    {
        p.Type = r.Type;
        p.Slug = r.Slug.Trim().ToLowerInvariant();
        p.Title = r.Title.Trim();
        p.Excerpt = r.Excerpt?.Trim();
        p.Body = sanitizer.Sanitize(r.Body);
        p.CoverImageUrl = r.CoverImageUrl;
        p.Author = r.Author?.Trim();
        p.AttachmentUrl = r.AttachmentUrl;
        p.AttachmentLabel = r.AttachmentLabel;
        p.IsFeatured = r.IsFeatured;
        p.ShowInTicker = r.ShowInTicker;
        p.ExpiresAt = r.ExpiresAt;
        p.MetaDescription = r.MetaDescription?.Trim();
    }
}

// --------------------------------------------------------------------- menu ----

[Route("admin/menu")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public class AdminMenuController(ApplicationDbContext db, IAuditService audit) : ApiControllerBase(db)
{
    /// <summary>Menu items for one location, as a nested tree.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminMenuItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminMenuItemDto>>> GetAll(
        [FromQuery] MenuLocation? location, CancellationToken ct)
    {
        var source = Db.MenuItems.AsNoTracking().Include(m => m.Page).AsQueryable();
        if (location is not null) source = source.Where(m => m.Location == location);

        var items = await source.OrderBy(m => m.SortOrder).ToListAsync(ct);

        foreach (var item in items)
            item.Children = items.Where(c => c.ParentId == item.Id).OrderBy(c => c.SortOrder).ToList();

        var byId = items.ToDictionary(m => m.Id);
        var roots = items
            .Where(m => m.ParentId is null || !byId.ContainsKey(m.ParentId.Value))
            .OrderBy(m => m.SortOrder)
            .Select(MappingExtensions.ToAdminDto)
            .ToList();

        return Ok(roots);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminMenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminMenuItemDto>> GetById(int id, CancellationToken ct)
    {
        var item = await Db.MenuItems.AsNoTracking().Include(m => m.Page)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return item is null ? NotFoundProblem("Menu item") : Ok(item.ToAdminDto());
    }

    [HttpPost]
    [ProducesResponseType<AdminMenuItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminMenuItemDto>> Create(
        [FromBody] SaveMenuItemRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(request, null, ct) is { } error) return BadRequestProblem(error);

        var item = new MenuItem();
        Apply(request, item);

        Db.MenuItems.Add(item);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Create", nameof(MenuItem), item.Id.ToString(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item.ToAdminDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminMenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminMenuItemDto>> Update(
        int id, [FromBody] SaveMenuItemRequest request, CancellationToken ct)
    {
        var item = await Db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (item is null) return NotFoundProblem("Menu item");

        if (await ValidateAsync(request, item, ct) is { } error) return BadRequestProblem(error);

        Apply(request, item);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Update", nameof(MenuItem), id.ToString(), request, ct);
        return Ok(item.ToAdminDto());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await Db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (item is null) return NotFoundProblem("Menu item");

        if (await Db.MenuItems.AnyAsync(m => m.ParentId == id, ct))
            return ConflictProblem("This menu item has sub-items. Delete or move them first.");

        item.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(MenuItem), id.ToString(), ct: ct);
        return NoContent();
    }

    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reorder([FromBody] ReorderRequest request, CancellationToken ct)
    {
        var ids = request.Items.Select(i => i.Id).ToList();
        var items = await Db.MenuItems.Where(m => ids.Contains(m.Id)).ToListAsync(ct);

        foreach (var change in request.Items)
        {
            if (items.FirstOrDefault(m => m.Id == change.Id) is { } item)
                item.SortOrder = change.SortOrder;
        }

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("Reorder", nameof(MenuItem), null, request.Items, ct);
        return NoContent();
    }

    private static void Apply(SaveMenuItemRequest r, MenuItem m)
    {
        m.Location = r.Location;
        m.Label = r.Label.Trim();
        m.Url = string.IsNullOrWhiteSpace(r.Url) ? null : r.Url.Trim();
        m.PageId = r.PageId;
        m.ParentId = r.ParentId;
        m.SortOrder = r.SortOrder;
        m.OpenInNewTab = r.OpenInNewTab;
        m.IsActive = r.IsActive;
        m.Icon = r.Icon;
        m.IsHighlighted = r.IsHighlighted;
    }

    private async Task<string?> ValidateAsync(SaveMenuItemRequest r, MenuItem? existing, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Url) && r.PageId is null)
            return "Provide either a URL or select a page for this menu item.";

        if (r.PageId is not null && !await Db.Pages.AnyAsync(p => p.Id == r.PageId, ct))
            return "The selected page does not exist.";

        if (r.ParentId is null) return null;

        if (existing is not null && r.ParentId == existing.Id)
            return "A menu item cannot be its own parent.";

        var parent = await Db.MenuItems.AsNoTracking()
            .Where(m => m.Id == r.ParentId)
            .Select(m => new { m.Id, m.Location, m.ParentId })
            .FirstOrDefaultAsync(ct);

        if (parent is null) return "The selected parent menu item does not exist.";
        if (parent.Location != r.Location) return "A menu item must sit under a parent in the same menu.";
        if (parent.ParentId is not null) return "Menus support two levels only.";

        return null;
    }
}

// ------------------------------------------------------------------ gallery ----

[Route("admin/gallery")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public class AdminGalleryController(ApplicationDbContext db, IAuditService audit) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<GalleryAlbumSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<GalleryAlbumSummaryDto>>> GetAlbums(
        [FromQuery] PagedQuery query, CancellationToken ct)
    {
        var source = Db.GalleryAlbums.AsNoTracking().Include(a => a.Images).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(a => EF.Functions.Like(a.Title, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderBy(a => a.SortOrder).ThenByDescending(a => a.EventDate)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<GalleryAlbumSummaryDto>.Create(
            items.Select(MappingExtensions.ToSummaryDto).ToList(), query.Page, query.PageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<GalleryAlbumDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GalleryAlbumDto>> GetAlbum(int id, CancellationToken ct)
    {
        var album = await Db.GalleryAlbums.AsNoTracking().Include(a => a.Images)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return album is null ? NotFoundProblem("Album") : Ok(album.ToDto());
    }

    [HttpPost]
    [ProducesResponseType<GalleryAlbumDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GalleryAlbumDto>> CreateAlbum(
        [FromBody] SaveGalleryAlbumRequest request, CancellationToken ct)
    {
        if (await Db.GalleryAlbums.AnyAsync(a => a.Slug == request.Slug, ct))
            return BadRequestProblem($"The slug '{request.Slug}' is already in use.");

        var album = new GalleryAlbum { Status = PublishStatus.Published, PublishedAt = DateTimeOffset.UtcNow };
        Apply(request, album);

        Db.GalleryAlbums.Add(album);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Create", nameof(GalleryAlbum), album.Id.ToString(), request, ct);
        return CreatedAtAction(nameof(GetAlbum), new { id = album.Id }, album.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<GalleryAlbumDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GalleryAlbumDto>> UpdateAlbum(
        int id, [FromBody] SaveGalleryAlbumRequest request, CancellationToken ct)
    {
        var album = await Db.GalleryAlbums.Include(a => a.Images).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (album is null) return NotFoundProblem("Album");

        if (await Db.GalleryAlbums.AnyAsync(a => a.Slug == request.Slug && a.Id != id, ct))
            return BadRequestProblem($"The slug '{request.Slug}' is already in use.");

        Apply(request, album);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Update", nameof(GalleryAlbum), id.ToString(), request, ct);
        return Ok(album.ToDto());
    }

    /// <summary>
    /// Takes an album off the public site, or puts it back. Deleting is the only other
    /// way to hide one, and that is not something an editor should have to do to a set
    /// of photographs they intend to show again.
    /// </summary>
    [HttpPost("{id:int}/status")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeAlbumStatus(
        int id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var album = await Db.GalleryAlbums.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (album is null) return NotFoundProblem("Album");

        album.Status = request.Status;
        if (request.Status == PublishStatus.Published)
            album.PublishedAt = request.PublishedAt ?? album.PublishedAt ?? DateTimeOffset.UtcNow;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("ChangeStatus", nameof(GalleryAlbum), id.ToString(), new { request.Status }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAlbum(int id, CancellationToken ct)
    {
        var album = await Db.GalleryAlbums.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (album is null) return NotFoundProblem("Album");

        album.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(GalleryAlbum), id.ToString(), ct: ct);
        return NoContent();
    }

    [HttpPost("{albumId:int}/images")]
    [ProducesResponseType<GalleryImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GalleryImageDto>> AddImage(
        int albumId, [FromBody] SaveGalleryImageRequest request, CancellationToken ct)
    {
        if (!await Db.GalleryAlbums.AnyAsync(a => a.Id == albumId, ct)) return NotFoundProblem("Album");

        var image = new GalleryImage
        {
            AlbumId = albumId,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            VideoUrl = request.VideoUrl,
            Caption = request.Caption,
            AltText = request.AltText,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        Db.GalleryImages.Add(image);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Create", nameof(GalleryImage), image.Id.ToString(), new { albumId }, ct);
        return CreatedAtAction(nameof(GetAlbum), new { id = albumId }, image.ToDto());
    }

    [HttpPut("{albumId:int}/images/{imageId:int}")]
    [ProducesResponseType<GalleryImageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GalleryImageDto>> UpdateImage(
        int albumId, int imageId, [FromBody] SaveGalleryImageRequest request, CancellationToken ct)
    {
        var image = await Db.GalleryImages.FirstOrDefaultAsync(i => i.Id == imageId && i.AlbumId == albumId, ct);
        if (image is null) return NotFoundProblem("Image");

        image.ImageUrl = request.ImageUrl;
        image.ThumbnailUrl = request.ThumbnailUrl;
        image.VideoUrl = request.VideoUrl;
        image.Caption = request.Caption;
        image.AltText = request.AltText;
        image.SortOrder = request.SortOrder;
        image.IsActive = request.IsActive;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("Update", nameof(GalleryImage), imageId.ToString(), new { albumId }, ct);
        return Ok(image.ToDto());
    }

    [HttpDelete("{albumId:int}/images/{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteImage(int albumId, int imageId, CancellationToken ct)
    {
        var image = await Db.GalleryImages.FirstOrDefaultAsync(i => i.Id == imageId && i.AlbumId == albumId, ct);
        if (image is null) return NotFoundProblem("Image");

        Db.GalleryImages.Remove(image);
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(GalleryImage), imageId.ToString(), new { albumId }, ct);
        return NoContent();
    }

    private static void Apply(SaveGalleryAlbumRequest r, GalleryAlbum a)
    {
        a.Slug = r.Slug.Trim().ToLowerInvariant();
        a.Title = r.Title.Trim();
        a.Description = r.Description?.Trim();
        a.CoverImageUrl = r.CoverImageUrl;
        a.Location = r.Location;
        a.EventDate = r.EventDate;
        a.SortOrder = r.SortOrder;
    }
}
