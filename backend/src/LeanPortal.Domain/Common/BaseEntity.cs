namespace LeanPortal.Domain.Common;

/// <summary>Base type for every persisted entity in the LEAN portal CMS.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}

/// <summary>Adds who/when auditing columns. Populated automatically by the DbContext.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>Entities that participate in the draft -> review -> publish workflow.</summary>
public interface IPublishable
{
    PublishStatus Status { get; set; }
    DateTimeOffset? PublishedAt { get; set; }
}

public abstract class AuditableEntity : BaseEntity, IAuditable
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

public enum PublishStatus
{
    Draft = 0,
    InReview = 1,
    Published = 2,
    Archived = 3
}
