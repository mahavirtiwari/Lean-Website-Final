using Microsoft.AspNetCore.Identity;

namespace LeanPortal.Domain.Identity;

/// <summary>CMS back-office user. Public MSME users live in the transactional LEAN system.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    /// <summary>Forces a password change on next sign-in (used for seeded/reset accounts).</summary>
    public bool MustChangePassword { get; set; }

    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}

/// <summary>Canonical role names used across the API authorisation policies.</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrator = "Administrator";
    public const string Editor = "Editor";
    public const string Publisher = "Publisher";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [SuperAdmin, Administrator, Editor, Publisher, Viewer];

    /// <summary>Roles allowed to create/update content.</summary>
    public const string CanEdit = $"{SuperAdmin},{Administrator},{Editor},{Publisher}";
    /// <summary>Roles allowed to move content to Published.</summary>
    public const string CanPublish = $"{SuperAdmin},{Administrator},{Publisher}";
    /// <summary>Roles allowed to manage users, roles and settings.</summary>
    public const string CanAdminister = $"{SuperAdmin},{Administrator}";
}
