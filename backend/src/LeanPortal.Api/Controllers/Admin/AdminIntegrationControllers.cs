using LeanPortal.Api.Infrastructure;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Admin;

/// <summary>
/// QCI's Zoho Desk connection: the account, the ticket's fields, the grievance
/// matrix, and a way to prove it all works before an enquiry depends on it.
/// </summary>
[Route("admin/helpdesk")]
// CanAdminister: this holds a client secret and a refresh token, and decides where
// grievances go.
[Authorize(Policy = Policies.CanAdminister)]
public class AdminHelpdeskController(
    ApplicationDbContext db,
    ISecretProtector secrets,
    IZohoDeskClient zoho,
    HelpdeskRetryService retries,
    IAuditService audit,
    ILogger<AdminHelpdeskController> logger) : ApiControllerBase(db)
{
    [HttpGet]
    [ProducesResponseType<AdminHelpdeskScreenDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminHelpdeskScreenDto>> Get(CancellationToken ct)
    {
        var connection = await LoadAsync(ct);

        var agencies = await Db.Partners.AsNoTracking()
            .Where(p => p.Type == PartnerType.ImplementationAgency)
            .OrderBy(p => p.SortOrder)
            .Select(p => new HelpdeskAgencyDto(p.Id, p.Name, p.ShortName))
            .ToListAsync(ct);

        return Ok(new AdminHelpdeskScreenDto(
            ToDto(connection),
            agencies,
            HelpdeskDispatcher.Placeholders.Select(p => new HelpdeskPlaceholderDto(p.Name, p.Meaning)).ToList(),
            await QueueAsync(ct),
            HelpdeskDispatcher.DefaultTicketTemplate));
    }

    [HttpPut]
    [ProducesResponseType<AdminHelpdeskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminHelpdeskDto>> Save([FromBody] SaveHelpdeskRequest request, CancellationToken ct)
    {
        var accounts = Https(request.AccountsUrl);
        var api = Https(request.ApiBaseUrl);
        if (accounts is null) return BadRequestProblem("The accounts server must be an https:// address, e.g. https://accounts.zoho.in.");
        if (api is null) return BadRequestProblem("The Desk API must be an https:// address, e.g. https://desk.zoho.in.");

        if (request.PartnerId is { } partnerId &&
            !await Db.Partners.AnyAsync(p => p.Id == partnerId && p.Type == PartnerType.ImplementationAgency, ct))
            return BadRequestProblem("Choose one of the implementing agencies.");

        if (JsonTemplate.Validate(request.TicketTemplate, HelpdeskDispatcher.Placeholders.Select(p => p.Name))
            is { } templateProblem)
            return BadRequestProblem("Ticket fields: " + templateProblem);

        if (GrievanceMatrix.Validate(request.GrievanceMatrix) is { } matrixProblem)
            return BadRequestProblem(matrixProblem);

        var connection = await LoadAsync(ct);

        connection.PartnerId = request.PartnerId;
        connection.AccountsUrl = accounts;
        connection.ApiBaseUrl = api;
        connection.OrganisationId = Clean(request.OrganisationId);
        connection.DepartmentId = Clean(request.DepartmentId);
        connection.ClientId = Clean(request.ClientId);
        connection.Channel = Clean(request.Channel) ?? "Web";
        connection.ContactOwnerId = Clean(request.ContactOwnerId);
        connection.TicketTemplate = Clean(request.TicketTemplate);
        connection.GrievanceMatrix = Clean(request.GrievanceMatrix);
        connection.SendAttachments = request.SendAttachments;
        connection.AlsoSendEmail = request.AlsoSendEmail;

        // An empty box keeps what is stored: the secrets never travel to the browser,
        // so the form cannot show them, and a blank cannot mean "delete".
        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
            connection.ClientSecret = secrets.Protect(request.ClientSecret.Trim());
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            connection.RefreshToken = secrets.Protect(request.RefreshToken.Trim());

        if (request.IsEnabled && !HelpdeskDispatcher.IsComplete(connection))
            return BadRequestProblem(
                "It cannot be switched on yet: the organisation id, department id, client id, client secret " +
                "and refresh token are all needed. Save it switched off until you have them.");

        if (request.IsEnabled && connection.PartnerId is null)
            return BadRequestProblem("Choose the agency whose enquiries become tickets.");

        connection.IsEnabled = request.IsEnabled;

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "HelpdeskConnection", connection.Id.ToString(), new
        {
            connection.IsEnabled,
            connection.PartnerId,
            connection.ApiBaseUrl,
            connection.OrganisationId,
            connection.DepartmentId,
            // Whether they changed, never what to.
            secretChanged = !string.IsNullOrWhiteSpace(request.ClientSecret),
            refreshTokenChanged = !string.IsNullOrWhiteSpace(request.RefreshToken),
        }, ct);

        return Ok(ToDto(connection));
    }

    /// <summary>
    /// Gets a token and reads the department back. Proves the credentials, the data
    /// centre, the organisation and the department all fit, without raising a ticket.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Test(CancellationToken ct)
    {
        var connection = await LoadAsync(ct);
        if (!HelpdeskDispatcher.IsComplete(connection))
            return BadRequestProblem("Save the organisation id, department id, client id, client secret and " +
                                     "refresh token first - the test uses what is saved.");

        string result;
        bool ok;
        try
        {
            var department = await zoho.CheckAsync(HelpdeskDispatcher.AccountFor(connection, secrets),
                connection.DepartmentId!, ct);
            ok = true;
            result = $"Connected. Tickets will be raised in the \"{department}\" department.";
        }
        // Every failure, not only the ones with a worded message: Zoho answering a
        // proxy's block page with a 200 throws while the body is parsed, and an
        // administrator pressing Test deserves that as a sentence and a recorded
        // result rather than as a 500 with nothing written down.
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            ok = false;
            result = ex is ZohoDeskException or HttpRequestException or TaskCanceledException
                ? ex.Message
                : $"The reply from Zoho could not be read ({ex.GetType().Name}: {ex.Message}).";
            logger.LogWarning(ex, "Zoho Desk connection test failed");
        }

        connection.LastCheckedAt = DateTimeOffset.UtcNow;
        connection.LastCheckSucceeded = ok;
        connection.LastCheckResult = result.Length > 1000 ? result[..1000] : result;
        await Db.SaveChangesAsync(ct);

        return ok ? Ok(new { message = result }) : BadRequestProblem(result);
    }

    /// <summary>What a ticket built with this template would carry, from a made-up enquiry.</summary>
    [HttpPost("preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Preview([FromBody] HelpdeskPreviewRequest request, CancellationToken ct)
    {
        if (JsonTemplate.Validate(request.TicketTemplate, HelpdeskDispatcher.Placeholders.Select(p => p.Name))
            is { } problem)
            return BadRequestProblem(problem);

        var connection = await LoadAsync(ct);
        var matrix = GrievanceMatrix.Parse("sample", connection.GrievanceMatrix);

        // The first path through the matrix, so the custom fields show real values.
        var path = new List<string?>();
        IReadOnlyList<GrievanceOptionDto>? level = matrix?.Options;
        while (level is { Count: > 0 } && path.Count < GrievanceMatrix.Levels)
        {
            path.Add(level[0].Name);
            level = level[0].Children;
        }

        var sample = new ContactMessage
        {
            Id = 123,
            Name = "Asha Devi Sharma",
            Email = "asha@example.com",
            Phone = "9876543210",
            Organisation = "Sharma Precision Components",
            UdyamNumber = "UDYAM-DL-01-0012345",
            State = "Delhi",
            Subject = "Sample enquiry",
            Message = "This is what an enquiry's message looks like in the ticket.",
            Agency = "QCI",
            UserType = path.ElementAtOrDefault(0),
            IssueType = path.ElementAtOrDefault(1),
            IssueCategory = path.ElementAtOrDefault(2),
            IssueSubCategory = path.ElementAtOrDefault(3),
        };

        var preview = HelpdeskDispatcher.BuildTicket(
            new HelpdeskConnection
            {
                DepartmentId = connection.DepartmentId ?? "(department id)",
                Channel = connection.Channel,
                TicketTemplate = request.TicketTemplate,
            },
            sample, "(the contact's id, found or created in Zoho)",
            connection.SendAttachments ? new System.Text.Json.Nodes.JsonArray("(attachment id)") : null);

        return Content(preview.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            "application/json");
    }

    /// <summary>
    /// Puts every enquiry that is waiting, or that gave up, back in the queue and
    /// runs it now. For after a fix - a replaced token, a corrected department.
    /// </summary>
    [HttpPost("retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Retry(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var requeued = await Db.ContactMessages
            .Where(m => m.HelpdeskStatus == HelpdeskStatus.Pending || m.HelpdeskStatus == HelpdeskStatus.Failed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.HelpdeskStatus, HelpdeskStatus.Pending)
                .SetProperty(m => m.HelpdeskNextAttemptAt, now)
                .SetProperty(m => m.HelpdeskAttempts, 0), ct);

        var sent = requeued > 0 ? await retries.SweepAsync(ct) : 0;

        await audit.LogAsync("RETRY", "HelpdeskConnection", null, new { requeued, sent }, ct);

        return Ok(new
        {
            message = requeued == 0
                ? "Nothing was waiting."
                : $"{sent} of {requeued} raised in Zoho Desk." +
                  (sent < requeued ? " The rest will keep being retried; the reason is shown below." : string.Empty),
            queue = await QueueAsync(ct),
        });
    }

    // ---------------------------------------------------------------- helpers ----

    /// <summary>There is one connection. It is created if the seed has not made it yet.</summary>
    private async Task<HelpdeskConnection> LoadAsync(CancellationToken ct)
    {
        var connection = await Db.HelpdeskConnections.OrderBy(c => c.Id).FirstOrDefaultAsync(ct);
        if (connection is not null) return connection;

        connection = new HelpdeskConnection { TicketTemplate = HelpdeskDispatcher.DefaultTicketTemplate };
        Db.HelpdeskConnections.Add(connection);
        await Db.SaveChangesAsync(ct);
        return connection;
    }

    private async Task<HelpdeskQueueDto> QueueAsync(CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);

        var waiting = await Db.ContactMessages.CountAsync(m => m.HelpdeskStatus == HelpdeskStatus.Pending, ct);
        var failed = await Db.ContactMessages.CountAsync(m => m.HelpdeskStatus == HelpdeskStatus.Failed, ct);
        var sent = await Db.ContactMessages.CountAsync(m => m.HelpdeskStatus == HelpdeskStatus.Sent && m.HelpdeskSentAt >= since, ct);
        var lastSent = await Db.ContactMessages.Where(m => m.HelpdeskStatus == HelpdeskStatus.Sent)
            .MaxAsync(m => (DateTimeOffset?)m.HelpdeskSentAt, ct);

        // The reason, not the enquiry: this screen shows why the queue is stuck, and
        // nothing of what anyone wrote.
        var lastError = await Db.ContactMessages
            .Where(m => m.HelpdeskLastError != null && m.HelpdeskStatus != HelpdeskStatus.Sent)
            .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .Select(m => new { m.HelpdeskLastError, At = m.UpdatedAt ?? m.CreatedAt })
            .FirstOrDefaultAsync(ct);

        return new HelpdeskQueueDto(waiting, failed, sent, lastSent, lastError?.HelpdeskLastError, lastError?.At);
    }

    private static AdminHelpdeskDto ToDto(HelpdeskConnection c) => new(
        c.Id, c.IsEnabled, c.PartnerId, c.AccountsUrl, c.ApiBaseUrl, c.OrganisationId, c.DepartmentId, c.ClientId,
        !string.IsNullOrEmpty(c.ClientSecret), !string.IsNullOrEmpty(c.RefreshToken), c.Channel, c.ContactOwnerId,
        c.TicketTemplate, c.GrievanceMatrix, c.SendAttachments, c.AlsoSendEmail,
        c.LastCheckedAt, c.LastCheckSucceeded, c.LastCheckResult);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Https(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
}

/// <summary>
/// Certificate verification, certified units and the assistant: which of a link, a
/// frame, a widget or an API each one is, and the details of whichever it is.
/// </summary>
[Route("admin/integrations")]
[Authorize(Policy = Policies.CanAdminister)]
public class AdminIntegrationsController(
    ApplicationDbContext db,
    ISecretProtector secrets,
    IIntegrationCaller caller,
    IAuditService audit) : ApiControllerBase(db)
{
    /// <summary>What each integration's API may use in its address and body.</summary>
    private static readonly Dictionary<string, string[]> PlaceholdersFor = new()
    {
        ["certificate-verification"] = ["certificateNumber", "query"],
        ["certified-units"] = ["search", "query", "page", "pageSize", "offset"],
        ["assistant"] = ["question", "query"],
    };

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminIntegrationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminIntegrationDto>>> GetAll(CancellationToken ct)
    {
        var rows = await Db.ExternalIntegrations.AsNoTracking().ToListAsync(ct);

        return Ok(PlaceholdersFor.Keys
            .Select(key => rows.FirstOrDefault(r => r.Key == key))
            .OfType<ExternalIntegration>()
            .Select(ToDto)
            .ToList());
    }

    [HttpPut("{key}")]
    [ProducesResponseType<AdminIntegrationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminIntegrationDto>> Save(
        string key, [FromBody] SaveIntegrationRequest request, CancellationToken ct)
    {
        if (!PlaceholdersFor.TryGetValue(key, out var placeholders)) return NotFound();

        var integration = await Db.ExternalIntegrations.FirstOrDefaultAsync(i => i.Key == key, ct);
        if (integration is null) return NotFound();

        // The built-in answers belong to the assistant alone.
        if (request.Mode == IntegrationMode.BuiltIn && key != "assistant")
            return BadRequestProblem("Only the assistant has built-in answers.");

        if (request.Mode is IntegrationMode.Link or IntegrationMode.Frame && IntegrationsSafeUrl(request.Url) is null)
            return BadRequestProblem("Give the full https:// address of the provider's page.");

        if (request.Mode == IntegrationMode.Embed && string.IsNullOrWhiteSpace(request.EmbedCode))
            return BadRequestProblem("Paste the embed code the provider gave you.");

        if (request.Mode == IntegrationMode.Api)
        {
            if (string.IsNullOrWhiteSpace(request.ApiUrl) ||
                !Uri.TryCreate(JsonTemplate.RenderUrl(request.ApiUrl.Trim(), new Dictionary<string, string?>()),
                    UriKind.Absolute, out var api) || api.Scheme is not ("https" or "http"))
                return BadRequestProblem("Give the full address of the API.");

            var unknown = JsonTemplate.PlaceholdersIn(request.ApiUrl)
                .Where(p => !placeholders.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
            if (unknown.Count > 0)
                return BadRequestProblem($"The address uses {string.Join(", ", unknown.Select(u => "{{" + u + "}}"))}, " +
                                         $"which this service does not fill. It can use: {string.Join(", ", placeholders.Select(p => "{{" + p + "}}"))}.");

            if (JsonTemplate.Validate(request.ApiBodyTemplate, placeholders) is { } bodyProblem)
                return BadRequestProblem("Request body: " + bodyProblem);

            if (!string.IsNullOrWhiteSpace(request.ApiFieldMap) &&
                IntegrationCaller.ParseMappings(request.ApiFieldMap).Count == 0)
                return BadRequestProblem("The columns could not be read. Each needs a label and a path.");
        }

        integration.Mode = request.Mode;
        integration.Title = Clean(request.Title);
        integration.Intro = Clean(request.Intro);
        integration.Url = Clean(request.Url);
        integration.EmbedCode = Clean(request.EmbedCode);
        integration.FrameHeight = Math.Clamp(request.FrameHeight <= 0 ? 720 : request.FrameHeight, 240, 2400);
        integration.ApiUrl = Clean(request.ApiUrl);
        integration.ApiMethod = string.Equals(request.ApiMethod, "POST", StringComparison.OrdinalIgnoreCase) ? "POST" : "GET";
        integration.ApiHeaderName = Clean(request.ApiHeaderName);
        integration.ApiBodyTemplate = Clean(request.ApiBodyTemplate);
        integration.ApiResultPath = Clean(request.ApiResultPath);
        integration.ApiFieldMap = Clean(request.ApiFieldMap);
        integration.ApiTotalPath = Clean(request.ApiTotalPath);
        integration.InputLabel = Clean(request.InputLabel);

        if (request.ClearApiHeaderValue) integration.ApiHeaderValue = null;
        else if (!string.IsNullOrWhiteSpace(request.ApiHeaderValue))
            integration.ApiHeaderValue = secrets.Protect(request.ApiHeaderValue.Trim());

        await Db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "ExternalIntegration", key, new
        {
            integration.Mode,
            integration.Url,
            integration.ApiUrl,
            keyChanged = request.ClearApiHeaderValue || !string.IsNullOrWhiteSpace(request.ApiHeaderValue),
        }, ct);

        return Ok(ToDto(integration));
    }

    /// <summary>Calls the saved API with a sample input and shows what the page would.</summary>
    [HttpPost("{key}/test")]
    [ProducesResponseType<IntegrationTestResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationTestResultDto>> Test(
        string key, [FromBody] IntegrationTestRequest request, CancellationToken ct)
    {
        if (!PlaceholdersFor.TryGetValue(key, out var placeholders)) return NotFound();

        var integration = await Db.ExternalIntegrations.FirstOrDefaultAsync(i => i.Key == key, ct);
        if (integration is null) return NotFound();

        var input = request.Input?.Trim() ?? string.Empty;
        var values = placeholders.ToDictionary(p => p, p => p switch
        {
            "page" => "1",
            "pageSize" => "20",
            "offset" => "0",
            _ => (string?)input,
        });

        var result = await caller.CallAsync(integration, values, ct);

        integration.LastCheckedAt = DateTimeOffset.UtcNow;
        integration.LastCheckSucceeded = result.Ok;
        integration.LastCheckResult = result.Ok ? $"Answered with {result.Rows.Count} result(s)." : result.Error;
        await Db.SaveChangesAsync(ct);

        return Ok(new IntegrationTestResultDto(result.Ok, result.Error, result.Status,
            result.Rows.Take(10).Select(r => new IntegrationRowDto(
                r.Select(v => new IntegrationValueDto(v.Key, v.Value)).ToList())).ToList(),
            result.Total, result.Raw));
    }

    private static AdminIntegrationDto ToDto(ExternalIntegration i) => new(
        i.Key, i.Mode, i.Title, i.Intro, i.Url, i.EmbedCode, i.FrameHeight, i.ApiUrl, i.ApiMethod, i.ApiHeaderName,
        !string.IsNullOrEmpty(i.ApiHeaderValue), i.ApiBodyTemplate, i.ApiResultPath, i.ApiFieldMap, i.ApiTotalPath,
        i.InputLabel, i.LastCheckedAt, i.LastCheckSucceeded, i.LastCheckResult,
        PlaceholdersFor.TryGetValue(i.Key, out var p) ? p : []);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? IntegrationsSafeUrl(string? url) => Public.IntegrationsController.SafeUrl(url);
}
