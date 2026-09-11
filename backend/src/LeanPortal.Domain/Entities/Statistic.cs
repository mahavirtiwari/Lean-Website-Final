using LeanPortal.Domain.Common;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A number on the "Portal Analytics" counter strip (MSMEs Registered, Pledges Taken, ...).
/// Values are editable in the CMS, and optionally refreshed from the transactional LEAN
/// database via <see cref="SourceQueryKey"/>.
/// </summary>
public class Statistic : AuditableEntity
{
    public string Label { get; set; } = string.Empty;
    public long Value { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? Icon { get; set; }
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Named query the sync job uses to refresh <see cref="Value"/>; null means manual.</summary>
    public string? SourceQueryKey { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
}
