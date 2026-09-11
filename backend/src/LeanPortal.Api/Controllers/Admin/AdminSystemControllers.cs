using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Common;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Domain.Identity;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services.Branding;
using LeanPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeanPortal.Api.Controllers.Admin;

// ---------------------------------------------------------------- enquiries ----

[Route("admin/enquiries")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
/// <summary>
/// Enquiries, for an operator who needs to reach them directly.
///
/// There is deliberately no screen for this in the console: enquiries are sent to
/// the agency the visitor chose and kept in the database, and the ministry asked
/// that they not be managed here. These endpoints remain as the way to get at the
/// record - and to export it - if the mail relay has been down or a submission has
/// to be traced. Nothing in the front end calls them.
/// </summary>
public class AdminEnquiriesController(ApplicationDbContext db, IAuditService audit) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminContactMessageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminContactMessageDto>>> GetAll(
        [FromQuery] PagedQuery query, [FromQuery] ContactMessageStatus? status, [FromQuery] string? category,
        CancellationToken ct)
    {
        var source = Db.ContactMessages.AsNoTracking().AsQueryable();

        if (status is not null) source = source.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(category)) source = source.Where(m => m.Category == category);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(m => EF.Functions.Like(m.Name, $"%{term}%")
                                       || EF.Functions.Like(m.Email, $"%{term}%")
                                       || EF.Functions.Like(m.Subject, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(m => m.CreatedAt)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<AdminContactMessageDto>.Create(
            items.Select(MappingExtensions.ToAdminDto).ToList(), query.Page, query.PageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminContactMessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminContactMessageDto>> GetById(int id, CancellationToken ct)
    {
        var message = await Db.ContactMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        return message is null ? NotFoundProblem("Enquiry") : Ok(message.ToAdminDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminContactMessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminContactMessageDto>> Update(
        int id, [FromBody] UpdateContactMessageRequest request, CancellationToken ct)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFoundProblem("Enquiry");

        var wasResponded = message.Status == ContactMessageStatus.Responded;

        message.Status = request.Status;
        message.AssignedTo = request.AssignedTo;
        message.InternalNotes = request.InternalNotes;

        if (request.Status == ContactMessageStatus.Responded && !wasResponded)
            message.RespondedAt = DateTimeOffset.UtcNow;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("Update", nameof(ContactMessage), id.ToString(), new { request.Status }, ct);
        return Ok(message.ToAdminDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanAdminister)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFoundProblem("Enquiry");

        message.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(ContactMessage), id.ToString(), ct: ct);
        return NoContent();
    }

    /// <summary>Exports the filtered enquiries as CSV for offline reporting.</summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export([FromQuery] ContactMessageStatus? status, CancellationToken ct)
    {
        var source = Db.ContactMessages.AsNoTracking().AsQueryable();
        if (status is not null) source = source.Where(m => m.Status == status);

        var rows = await source.OrderByDescending(m => m.CreatedAt).Take(5000).ToListAsync(ct);

        static string Escape(string? value) =>
            value is null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        var csv = new System.Text.StringBuilder()
            .AppendLine("Reference,Received,Name,Email,Phone,Organisation,Udyam,State,Category,Subject,Status,AssignedTo");

        foreach (var m in rows)
        {
            csv.AppendLine(string.Join(',',
                Escape($"LEAN-ENQ-{m.Id:D6}"),
                Escape(m.CreatedAt.ToString("yyyy-MM-dd HH:mm")),
                Escape(m.Name), Escape(m.Email), Escape(m.Phone), Escape(m.Organisation),
                Escape(m.UdyamNumber), Escape(m.State), Escape(m.Category), Escape(m.Subject),
                Escape(m.Status.ToString()), Escape(m.AssignedTo)));
        }

        await audit.LogAsync("Export", nameof(ContactMessage), null, new { count = rows.Count, status }, ct);

        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv",
            $"lean-enquiries-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }
}

// ----------------------------------------------------------------- settings ----

[Route("admin/settings")]
[Authorize(Policy = Policies.CanAdminister)]
[OutputCache(NoStore = true)]
public class AdminSettingsController(
    ApplicationDbContext db,
    IAuditService audit,
    ISiteSettingsProvider settings,
    ISecretProtector secrets) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminSettingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminSettingDto>>> GetAll(
        [FromQuery] string? group, CancellationToken ct)
    {
        var source = Db.SiteSettings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(group)) source = source.Where(s => s.Group == group);

        var items = await source.OrderBy(s => s.Group).ThenBy(s => s.SortOrder).ToListAsync(ct);

        // A password never travels to the browser, not even to the administrator who
        // set it. The field arrives empty with a note saying one is stored, which is
        // all anyone needs in order to decide whether to replace it.
        foreach (var item in items.Where(i => i.DataType == "password"))
        {
            item.Description = string.IsNullOrEmpty(item.Value)
                ? item.Description
                : "A password is saved. Leave this empty to keep it, or type a new one to replace it.";
            item.Value = string.Empty;
        }

        return Ok(items.Select(MappingExtensions.ToAdminDto).ToList());
    }

    /// <summary>The colour themes on offer, for the Branding screen's swatches.</summary>
    [HttpGet("themes")]
    [ProducesResponseType<IReadOnlyList<ThemePreset>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ThemePreset>> GetThemes() => Ok(ThemePalette.Presets);

    [HttpGet("groups")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetGroups(CancellationToken ct) =>
        Ok(await Db.SiteSettings.AsNoTracking().Select(s => s.Group).Distinct().OrderBy(g => g).ToListAsync(ct));

    /// <summary>Bulk-saves a settings tab. Unknown keys are rejected rather than silently created.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Save([FromBody] SaveSettingsRequest request, CancellationToken ct)
    {
        var keys = request.Values.Keys.ToList();
        var existing = await Db.SiteSettings.Where(s => keys.Contains(s.Key)).ToListAsync(ct);

        var unknown = keys.Except(existing.Select(s => s.Key)).ToList();
        if (unknown.Count > 0)
            return BadRequestProblem($"Unknown setting keys: {string.Join(", ", unknown)}");

        // The values the site puts into links, images and its stylesheet are checked
        // here, not only in the screen that sets them: any of these could otherwise
        // put a javascript: link in the masthead or an unreadable theme on every page.
        foreach (var (key, value) in request.Values)
        {
            if (SettingRules.Check(key, value) is { } problem)
                return BadRequestProblem(problem);
        }

        if (keys.Any(k => k.StartsWith("theme.", StringComparison.Ordinal)))
        {
            // The theme is judged whole: the accent, the dark colour and the choice
            // between preset and custom may arrive in separate saves.
            var stored = await Db.SiteSettings.AsNoTracking()
                .Where(s => s.Key.StartsWith("theme."))
                .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
            string? Value(string key) => request.Values.TryGetValue(key, out var v) ? v?.Trim() : stored.GetValueOrDefault(key);

            var preset = Value("theme.preset");
            if (!ThemePalette.IsKnown(preset))
                return BadRequestProblem($"There is no theme called \"{preset}\".");

            if (preset == ThemePalette.Custom &&
                ThemePalette.Validate(Value("theme.customPrimary"), Value("theme.customDark")) is { } themeProblem)
                return BadRequestProblem(themeProblem);
        }

        foreach (var setting in existing)
        {
            var incoming = request.Values[setting.Key];

            if (setting.DataType == "password")
            {
                // Empty means "leave it alone": the field is always sent empty because
                // the value is never given out, so saving the tab must not wipe it.
                if (string.IsNullOrWhiteSpace(incoming)) continue;

                setting.Value = secrets.Protect(incoming.Trim());
                continue;
            }

            setting.Value = incoming;
        }

        await Db.SaveChangesAsync(ct);
        settings.Invalidate();

        // Keys only. The audit trail is read by more people than the settings screen.
        await audit.LogAsync("Update", nameof(SiteSetting), null, new { keys }, ct);
        return NoContent();
    }
}

// -------------------------------------------------------------------- media ----

[Route("admin/media")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public class AdminMediaController(
    ApplicationDbContext db,
    IFileStorage storage,
    IAuditService audit,
    IOptions<FileStorageOptions> storageOptions,
    ILogger<AdminMediaController> logger) : ApiControllerBase(db)
{
    private readonly FileStorageOptions _options = storageOptions.Value;

    [HttpGet]
    [ProducesResponseType<PagedResult<MediaAssetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MediaAssetDto>>> GetAll(
        [FromQuery] PagedQuery query, [FromQuery] string? folder, CancellationToken ct)
    {
        var source = Db.MediaAssets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(folder)) source = source.Where(m => m.Folder == folder);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(m => EF.Functions.Like(m.FileName, $"%{term}%")
                                       || EF.Functions.Like(m.AltText ?? string.Empty, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(m => m.CreatedAt)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<MediaAssetDto>.Create(
            items.Select(MappingExtensions.ToDto).ToList(), query.Page, query.PageSize, total));
    }

    [HttpGet("folders")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetFolders(CancellationToken ct) =>
        Ok(await Db.MediaAssets.AsNoTracking()
            .Where(m => m.Folder != null)
            .Select(m => m.Folder!).Distinct().OrderBy(f => f).ToListAsync(ct));

    [HttpPost("upload")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<MediaAssetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MediaAssetDto>> Upload(
        IFormFile file, [FromForm] string? folder, [FromForm] string? altText, [FromForm] string? caption,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequestProblem("Select a file to upload.");

        if (!storage.IsAllowed(file.FileName, file.ContentType, out var reason))
            return BadRequestProblem(reason ?? "This file type is not permitted.");

        var isImage = LocalFileStorage.IsImage(file.FileName);
        var limit = isImage ? _options.MaxImageBytes : _options.MaxDocumentBytes;

        if (file.Length > limit)
            return BadRequestProblem($"The file exceeds the {limit / (1024 * 1024)} MB limit for this file type.");

        await using var stream = file.OpenReadStream();
        var stored = await storage.SaveAsync(stream, file.FileName, file.ContentType, folder ?? "general", ct);

        var asset = new MediaAsset
        {
            FileName = Path.GetFileName(file.FileName),
            StoredFileName = stored.StoredFileName,
            Url = stored.Url,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            Checksum = stored.Checksum,
            AltText = altText,
            Caption = caption,
            Folder = folder ?? "general"
        };

        Db.MediaAssets.Add(asset);
        await Db.SaveChangesAsync(ct);

        logger.LogInformation("Media asset {Id} uploaded: {File}", asset.Id, asset.FileName);
        await audit.LogAsync("Upload", nameof(MediaAsset), asset.Id.ToString(), new { asset.FileName }, ct);

        return CreatedAtAction(nameof(GetAll), new { }, asset.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<MediaAssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetDto>> Update(
        int id, [FromBody] UpdateMediaAssetRequest request, CancellationToken ct)
    {
        var asset = await Db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (asset is null) return NotFoundProblem("Media asset");

        asset.AltText = request.AltText;
        asset.Caption = request.Caption;
        if (!string.IsNullOrWhiteSpace(request.Folder)) asset.Folder = request.Folder;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("Update", nameof(MediaAsset), id.ToString(), request, ct);
        return Ok(asset.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanPublish)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var asset = await Db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (asset is null) return NotFoundProblem("Media asset");

        await storage.DeleteAsync(asset.Url, ct);

        asset.IsDeleted = true;
        await Db.SaveChangesAsync(ct);

        await audit.LogAsync("Delete", nameof(MediaAsset), id.ToString(), new { asset.FileName }, ct);
        return NoContent();
    }
}

// -------------------------------------------------------------------- users ----

[Route("admin/users")]
[Authorize(Policy = Policies.CanAdminister)]
[OutputCache(NoStore = true)]
public class AdminUsersController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IAuditService audit,
    ICurrentUser currentUser) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetAll(
        [FromQuery] PagedQuery query, CancellationToken ct)
    {
        var source = Db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(u => EF.Functions.Like(u.FullName, $"%{term}%")
                                       || EF.Functions.Like(u.Email!, $"%{term}%"));
        }

        var total = await source.CountAsync(ct);

        var users = await source
            .OrderBy(u => u.FullName)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
            items.Add(await ToDtoAsync(user));

        return Ok(PagedResult<AdminUserDto>.Create(items, query.Page, query.PageSize, total));
    }

    [HttpGet("roles")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetRoles(CancellationToken ct) =>
        Ok(await roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync(ct));

    [HttpGet("{id}")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> GetById(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        return user is null ? NotFoundProblem("User") : Ok(await ToDtoAsync(user));
    }

    [HttpPost]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequestProblem("An account already exists for that e-mail address.");

        if (await ValidateRolesAsync(request.Roles) is { } roleError)
            return BadRequestProblem(roleError);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Designation = request.Designation,
            Department = request.Department,
            IsActive = true,
            MustChangePassword = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequestProblem(string.Join(" ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRolesAsync(user, request.Roles);
        await audit.LogAsync("Create", nameof(ApplicationUser), user.Id,
            new { request.Email, request.Roles }, ct);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await ToDtoAsync(user));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> Update(
        string id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFoundProblem("User");

        if (await ValidateRolesAsync(request.Roles) is { } roleError)
            return BadRequestProblem(roleError);

        // Guard against an administrator locking themselves out.
        if (user.Id == currentUser.UserId && !request.IsActive)
            return BadRequestProblem("You cannot deactivate your own account.");

        if (user.Id == currentUser.UserId
            && !request.Roles.Any(r => r is Roles.SuperAdmin or Roles.Administrator))
        {
            return BadRequestProblem("You cannot remove your own administrative role.");
        }

        user.FullName = request.FullName;
        user.Designation = request.Designation;
        user.Department = request.Department;
        user.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
        }

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return BadRequestProblem(string.Join(" ", update.Errors.Select(e => e.Description)));

        var current = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, current.Except(request.Roles));
        await userManager.AddToRolesAsync(user, request.Roles.Except(current));

        await audit.LogAsync("Update", nameof(ApplicationUser), user.Id, new { request.Roles, request.IsActive }, ct);
        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResetPassword(
        string id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFoundProblem("User");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return BadRequestProblem(string.Join(" ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = request.MustChangePassword;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await userManager.UpdateAsync(user);

        await audit.LogAsync("ResetPassword", nameof(ApplicationUser), user.Id, ct: ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFoundProblem("User");

        if (user.Id == currentUser.UserId)
            return BadRequestProblem("You cannot delete your own account.");

        // Deactivate rather than hard-delete, so the audit trail keeps its author references.
        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await userManager.UpdateAsync(user);

        await audit.LogAsync("Deactivate", nameof(ApplicationUser), user.Id, ct: ct);
        return NoContent();
    }

    private async Task<string?> ValidateRolesAsync(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                return $"The role '{role}' does not exist.";
        }

        return null;
    }

    private async Task<AdminUserDto> ToDtoAsync(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.FullName, user.Designation, user.Department,
            user.IsActive, user.MustChangePassword, user.CreatedAt, user.LastLoginAt,
            (await userManager.GetRolesAsync(user)).ToList());
}

// ---------------------------------------------------------------- dashboard ----

[Route("admin/dashboard")]
[Authorize(Policy = Policies.CanEdit)]
[OutputCache(NoStore = true)]
public class AdminDashboardController(ApplicationDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var recentPages = await Db.Pages.AsNoTracking().Include(p => p.Parent)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Take(6).ToListAsync(ct);

        var recentEnquiries = await Db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(6).ToListAsync(ct);

        var recentActivity = await Db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(12).ToListAsync(ct);

        return Ok(new DashboardDto(
            PublishedPages: await Db.Pages.CountAsync(p => p.Status == PublishStatus.Published, ct),
            DraftPages: await Db.Pages.CountAsync(p => p.Status != PublishStatus.Published, ct),
            PublishedPosts: await Db.Posts.CountAsync(p => p.Status == PublishStatus.Published, ct),
            Documents: await Db.Documents.CountAsync(ct),
            NewEnquiries: await Db.ContactMessages.CountAsync(m => m.Status == ContactMessageStatus.New, ct),
            TotalEnquiries: await Db.ContactMessages.CountAsync(ct),
            GalleryImages: await Db.GalleryImages.CountAsync(ct),
            UpcomingProgrammes: await Db.AwarenessProgrammes.CountAsync(p => p.StartDate > now, ct),
            ActiveUsers: await Db.Users.CountAsync(u => u.IsActive, ct),
            RecentlyUpdatedPages: recentPages.Select(MappingExtensions.ToListItemDto).ToList(),
            RecentEnquiries: recentEnquiries.Select(MappingExtensions.ToAdminDto).ToList(),
            RecentActivity: recentActivity.Select(MappingExtensions.ToDto).ToList()));
    }

    [HttpGet("activity")]
    [Authorize(Policy = Policies.CanAdminister)]
    [ProducesResponseType<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetActivity(
        [FromQuery] PagedQuery query, [FromQuery] string? entityName, CancellationToken ct)
    {
        var source = Db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) source = source.Where(a => a.EntityName == entityName);

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(a => a.Timestamp)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(PagedResult<AuditLogDto>.Create(
            items.Select(MappingExtensions.ToDto).ToList(), query.Page, query.PageSize, total));
    }
}

/// <summary>
/// What a setting may hold, for the settings that end up in a link, an image or
/// the stylesheet. Everything else is free text.
/// </summary>
public static class SettingRules
{
    private static readonly HashSet<string> Links =
    [
        "site.ministryLogoLink", "site.logoLink", "site.footerLogoLink", "site.footerLogo2Link",
    ];

    private static readonly HashSet<string> Images =
    [
        "site.ministryLogoUrl", "site.logoUrl", "site.ministryLogoWhiteUrl", "site.footerLogo2Url", "site.faviconUrl",
    ];

    public static string? Check(string key, string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v)) return null;

        if (Links.Contains(key) && !IsSitePath(v) && !IsWebAddress(v))
            return $"{key}: a logo can open a page on this site (starting with /) or a full http:// or https:// address.";

        if (Images.Contains(key) && !IsSitePath(v) && !IsWebAddress(v, httpsOnly: true))
            return $"{key}: a logo must be an image uploaded to the portal, or a full https:// address.";

        if (key is "theme.customPrimary" or "theme.customDark" && !ThemePalette.IsColour(v))
            return $"{key}: write the colour as #rrggbb, e.g. #1d5fa6.";

        return null;
    }

    /// <summary>A path on this site. Not "//host", which a browser reads as another site.</summary>
    private static bool IsSitePath(string v) => v.StartsWith('/') && !v.StartsWith("//") && !v.Contains('\\');

    private static bool IsWebAddress(string v, bool httpsOnly = false) =>
        Uri.TryCreate(v, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || (!httpsOnly && uri.Scheme == Uri.UriSchemeHttp));
}
