using LeanPortal.Domain.Enums;

namespace LeanPortal.Application.Contracts;

// ------------------------------------------------------------------ public ----

/// <summary>
/// How the page should present an external service. Deliberately without the API
/// address, its key or the embed code: none of those is the browser's business.
/// </summary>
public record PublicIntegrationDto(
    string Key,
    IntegrationMode Mode,
    string? Title,
    string? Intro,
    string? Url,
    int FrameHeight,
    string? InputLabel,
    IReadOnlyList<string> Columns);

/// <summary>One result: its values, in the order of the columns.</summary>
public record IntegrationRowDto(IReadOnlyList<IntegrationValueDto> Values);

public record IntegrationValueDto(string Label, string? Value);

public record IntegrationLookupDto(bool Found, string? Message, IReadOnlyList<IntegrationRowDto> Rows, long Total);

/// <summary>The grievance matrix an agency asks the contact form to use.</summary>
public record GrievanceMatrixDto(string Agency, IReadOnlyList<string> Labels, IReadOnlyList<GrievanceOptionDto> Options);

public record GrievanceOptionDto(string Name, IReadOnlyList<GrievanceOptionDto>? Children);

/// <summary>
/// What the contact form needs beyond the site settings: whether to ask which
/// agency the enquiry is for, the agency it goes to when it does not ask, and the
/// grievance matrix of any agency that uses one.
/// </summary>
public record ContactOptionsDto(
    IReadOnlyList<GrievanceMatrixDto> Grievance,
    bool AskAgency = true,
    string? DefaultAgency = null);

// ------------------------------------------------------------------- admin ----

public record AdminHelpdeskDto(
    int Id,
    bool IsEnabled,
    int? PartnerId,
    string AccountsUrl,
    string ApiBaseUrl,
    string? OrganisationId,
    string? DepartmentId,
    string? ClientId,
    bool HasClientSecret,
    bool HasRefreshToken,
    string Channel,
    string? ContactOwnerId,
    string? TicketTemplate,
    string? GrievanceMatrix,
    bool SendAttachments,
    bool AlsoSendEmail,
    DateTimeOffset? LastCheckedAt,
    bool? LastCheckSucceeded,
    string? LastCheckResult);

public record HelpdeskAgencyDto(int Id, string Name, string? ShortName);

public record HelpdeskPlaceholderDto(string Name, string Meaning);

/// <summary>How the queue stands: what is waiting, what has given up, what went through.</summary>
public record HelpdeskQueueDto(
    int Waiting,
    int Failed,
    int SentLast30Days,
    DateTimeOffset? LastSentAt,
    string? LastError,
    DateTimeOffset? LastErrorAt);

public record AdminHelpdeskScreenDto(
    AdminHelpdeskDto Connection,
    IReadOnlyList<HelpdeskAgencyDto> Agencies,
    IReadOnlyList<HelpdeskPlaceholderDto> Placeholders,
    HelpdeskQueueDto Queue,
    string DefaultTicketTemplate);

/// <summary>
/// A save. A secret left empty keeps the one already stored - it is never sent
/// back to the browser, so an empty box means "unchanged", not "remove it".
/// </summary>
public record SaveHelpdeskRequest(
    bool IsEnabled,
    int? PartnerId,
    string AccountsUrl,
    string ApiBaseUrl,
    string? OrganisationId,
    string? DepartmentId,
    string? ClientId,
    string? ClientSecret,
    string? RefreshToken,
    string? Channel,
    string? ContactOwnerId,
    string? TicketTemplate,
    string? GrievanceMatrix,
    bool SendAttachments,
    bool AlsoSendEmail);

public record HelpdeskPreviewRequest(string? TicketTemplate);

public record AdminIntegrationDto(
    string Key,
    IntegrationMode Mode,
    string? Title,
    string? Intro,
    string? Url,
    string? EmbedCode,
    int FrameHeight,
    string? ApiUrl,
    string ApiMethod,
    string? ApiHeaderName,
    bool HasApiHeaderValue,
    string? ApiBodyTemplate,
    string? ApiResultPath,
    string? ApiFieldMap,
    string? ApiTotalPath,
    string? InputLabel,
    DateTimeOffset? LastCheckedAt,
    bool? LastCheckSucceeded,
    string? LastCheckResult,
    IReadOnlyList<string> Placeholders);

public record SaveIntegrationRequest(
    IntegrationMode Mode,
    string? Title,
    string? Intro,
    string? Url,
    string? EmbedCode,
    int FrameHeight,
    string? ApiUrl,
    string? ApiMethod,
    string? ApiHeaderName,
    string? ApiHeaderValue,
    bool ClearApiHeaderValue,
    string? ApiBodyTemplate,
    string? ApiResultPath,
    string? ApiFieldMap,
    string? ApiTotalPath,
    string? InputLabel);

public record IntegrationTestRequest(string? Input);

public record IntegrationTestResultDto(
    bool Ok,
    string? Error,
    int? Status,
    IReadOnlyList<IntegrationRowDto> Rows,
    long? Total,
    string? Raw);
