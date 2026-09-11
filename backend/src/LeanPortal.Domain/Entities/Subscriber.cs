using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>Newsletter subscription captured from the footer sign-up.</summary>
public class Subscriber : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool IsConfirmed { get; set; }
    public string? ConfirmationToken { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? UnsubscribedAt { get; set; }
    public string? Source { get; set; }
}
