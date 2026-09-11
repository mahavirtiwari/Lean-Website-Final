using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>Immutable record of every admin write, required for government audit trails.</summary>
public class AuditLog : BaseEntity
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Changes { get; set; }
    public string? IpAddress { get; set; }
}
