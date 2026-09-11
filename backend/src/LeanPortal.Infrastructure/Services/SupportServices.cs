using System.Security.Claims;
using System.Text.Json;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services;

/// <summary>Reads the caller from the current HTTP context for audit stamping.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name) ?? Principal?.Identity?.Name;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}

/// <summary>Persists the admin audit trail.</summary>
public class AuditService(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    IHttpContextAccessor accessor,
    ILogger<AuditService> logger) : IAuditService
{
    public async Task LogAsync(string action, string entityName, string? entityId, object? changes = null,
        CancellationToken ct = default)
    {
        try
        {
            db.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                IpAddress = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Changes = changes is null ? null : JsonSerializer.Serialize(changes)
            });

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // An audit failure must never take down the operation the user was performing.
            logger.LogError(ex, "Failed to write audit entry for {Action} {Entity} {EntityId}",
                action, entityName, entityId);
        }
    }
}

/// <summary>Serves the public site settings from an in-memory cache.</summary>
public class SiteSettingsProvider(ApplicationDbContext db, IMemoryCache cache) : ISiteSettingsProvider
{
    private const string CacheKey = "site-settings:public";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyDictionary<string, string?>> GetPublicSettingsAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, string?>? cached) && cached is not null)
            return cached;

        var settings = await db.SiteSettings
            .AsNoTracking()
            .Where(s => s.IsPublic)
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        cache.Set(CacheKey, (IReadOnlyDictionary<string, string?>)settings, CacheDuration);
        return settings;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var settings = await GetPublicSettingsAsync(ct);
        if (settings.TryGetValue(key, out var value)) return value;

        return await db.SiteSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsEnabledAsync(string key, CancellationToken ct = default, bool fallback = true)
    {
        var value = await GetAsync(key, ct);
        return bool.TryParse(value, out var enabled) ? enabled : fallback;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}

