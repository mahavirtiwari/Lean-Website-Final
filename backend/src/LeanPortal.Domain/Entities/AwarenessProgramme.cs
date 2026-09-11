using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>Awareness programme / assessor / consultant training event listing.</summary>
public class AwarenessProgramme : AuditableEntity, IPublishable
{
    public string ProgrammeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Awareness Programme, Assessor Training, Consultant Training.</summary>
    public string ProgrammeType { get; set; } = "Awareness Programme";
    /// <summary>Implementing agency - QCI or NPC.</summary>
    public string? Agency { get; set; }

    public string? State { get; set; }
    public string? District { get; set; }
    public string? Venue { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    public int RegisteredCount { get; set; }
    public int? Capacity { get; set; }
    public string? RegistrationUrl { get; set; }
    public ProgrammeStatus ProgrammeStatus { get; set; } = ProgrammeStatus.Upcoming;

    public PublishStatus Status { get; set; } = PublishStatus.Published;
    public DateTimeOffset? PublishedAt { get; set; }
}
