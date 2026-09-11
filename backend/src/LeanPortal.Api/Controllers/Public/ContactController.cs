using System.ComponentModel.DataAnnotations;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Public;

/// <summary>Public form endpoints: enquiries and newsletter sign-up.</summary>
[Route("")]
[EnableRateLimiting("forms")]
[OutputCache(NoStore = true)]
public class ContactController(
    ApplicationDbContext db,
    IEmailSender email,
    ISiteSettingsProvider settings,
    ICaptchaService captcha,
    IEnquiryRouter router,
    IFileStorage storage,
    IHelpdeskDispatcher helpdesk,
    ILogger<ContactController> logger) : ApiControllerBase(db)
{
    /// <summary>At most three files of five megabytes each, as the form says.</summary>
    private const int MaxAttachments = 3;

    private const long MaxAttachmentBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Hands out a challenge - for the enquiry form and the console's sign-in. A
    /// picture by default; <c>?type=question</c> gives the written question for
    /// anyone who cannot see it.
    /// </summary>
    [HttpGet("contact/captcha")]
    // Its own allowance. Under the form's five a minute, a visitor who pressed
    // refresh a few times, or switched to the question, was told "too many
    // requests" before sending anything.
    [EnableRateLimiting("captcha")]
    [ProducesResponseType<CaptchaChallenge>(StatusCodes.Status200OK)]
    public ActionResult<CaptchaChallenge> Captcha([FromQuery] string? type) =>
        Ok(string.Equals(type, "question", StringComparison.OrdinalIgnoreCase) ? captcha.IssueQuestion() : captcha.Issue());

    /// <summary>
    /// What the form needs to know beyond the site settings: which agencies classify
    /// an enquiry with a grievance matrix, and the matrix each one uses.
    /// </summary>
    [HttpGet("contact/options")]
    [EnableRateLimiting("public")]
    [ProducesResponseType<ContactOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContactOptionsDto>> Options(CancellationToken ct)
    {
        var rows = await Db.HelpdeskConnections.AsNoTracking()
            .Where(c => c.PartnerId != null && c.GrievanceMatrix != null && c.Partner!.IsActive)
            .Select(c => new { Agency = c.Partner!.ShortName ?? c.Partner.Name, c.GrievanceMatrix })
            .ToListAsync(ct);

        var matrices = rows
            .Select(r => GrievanceMatrix.Parse(r.Agency, r.GrievanceMatrix))
            .OfType<GrievanceMatrixDto>()
            .ToList();

        var choice = await AgencyChoiceAsync(ct);
        return Ok(new ContactOptionsDto(matrices, choice.Ask, choice.Default));
    }

    /// <summary>
    /// Whether the form asks which agency an enquiry is for, and where it goes
    /// when it does not.
    ///
    /// The question is asked when the console says so and there is more than one
    /// agency to choose between; a choice of one is not a question. When it is not
    /// asked, the enquiry goes to the agency named in the console, or the only
    /// active one, or - with neither - to the general enquiry address. Decided here
    /// so the form and the submission cannot disagree.
    /// </summary>
    private async Task<(bool Ask, string? Default, IReadOnlyList<string> Agencies)> AgencyChoiceAsync(CancellationToken ct)
    {
        var agencies = await Db.Partners.AsNoTracking()
            .Where(p => p.Type == PartnerType.ImplementationAgency && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.ShortName ?? p.Name)
            .ToListAsync(ct);

        var ask = await settings.IsEnabledAsync("feature.contactAgencyChoice", ct, fallback: true) && agencies.Count > 1;
        if (ask) return (true, null, agencies);

        var named = (await settings.GetAsync("contact.defaultAgency", ct))?.Trim();
        var chosen = agencies.FirstOrDefault(a => string.Equals(a, named, StringComparison.OrdinalIgnoreCase))
                     ?? (agencies.Count == 1 ? agencies[0] : null);

        return (false, chosen, agencies);
    }

    /// <summary>Records a "Get in touch" enquiry for triage in the admin console.</summary>
    [HttpPost("contact")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Submit(
        [FromForm] ContactRequest request,
        [FromForm] IFormFileCollection? attachments,
        CancellationToken ct)
    {
        // Honeypot: a real visitor never sees this field, so anything in it is a bot.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            logger.LogInformation("Rejected honeypot enquiry submission from {Ip}", ClientIp);
            return Accepted(new { message = "Thank you. Your enquiry has been received." });
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            return BadRequestProblem("Please provide your name.");

        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
            return BadRequestProblem("Please provide a valid e-mail address.");

        if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 400)
            return BadRequestProblem("Please provide a subject for your enquiry.");

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 4000)
            return BadRequestProblem("Please provide a message of up to 4000 characters.");

        // After the cheap checks and before anything is written or stored: a failed
        // challenge should cost nothing.
        if (!captcha.Validate(request.CaptchaId, request.CaptchaAnswer))
            return BadRequestProblem("The characters did not match. Please try the new image.");

        // An agency that classifies its enquiries with a grievance matrix needs all
        // four levels, and needs them to be options it actually has: they go into
        // the helpdesk's custom fields, which accept nothing else.
        // With the question asked, the answer must be one of the agencies offered.
        // Without it, the console's choice applies whatever was sent.
        var choice = await AgencyChoiceAsync(ct);
        var agency = choice.Ask
            ? choice.Agencies.FirstOrDefault(a => string.Equals(a, request.Agency?.Trim(), StringComparison.OrdinalIgnoreCase))
            : choice.Default;
        if (choice.Ask && agency is null)
            return BadRequestProblem("Please choose the agency your enquiry is for.");

        var matrix = await MatrixForAsync(agency, ct);
        if (matrix is not null && !GrievanceMatrix.IsValidPath(matrix,
                request.UserType, request.IssueType, request.IssueCategory, request.IssueSubCategory))
            return BadRequestProblem($"Please choose from each of the lists: {string.Join(", ", matrix.Labels)}.");

        var stored = new List<EnquiryAttachmentDto>();

        if (attachments is { Count: > 0 })
        {
            if (attachments.Count > MaxAttachments)
                return BadRequestProblem($"Please attach no more than {MaxAttachments} files.");

            foreach (var file in attachments)
            {
                if (file.Length == 0) continue;

                if (file.Length > MaxAttachmentBytes)
                    return BadRequestProblem($"{file.FileName} is larger than 5 MB.");

                if (!storage.IsAllowed(file.FileName, file.ContentType, out var reason))
                    return BadRequestProblem(reason ?? $"{file.FileName} is not an accepted file type.");

                await using var content = file.OpenReadStream();
                var saved = await storage.SaveAsync(content, file.FileName, file.ContentType, "enquiries", ct);

                stored.Add(new EnquiryAttachmentDto(file.FileName, saved.Url, saved.SizeBytes));
            }
        }

        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone?.Trim(),
            Organisation = request.Organisation?.Trim(),
            UdyamNumber = request.UdyamNumber?.Trim(),
            State = request.State?.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            // With a matrix the category is its first real division - complaint or
            // query - which is what someone scanning a list of enquiries wants.
            Category = matrix is not null ? request.IssueType?.Trim() : request.Category?.Trim(),
            Agency = agency,
            UserType = matrix is not null ? request.UserType?.Trim() : null,
            IssueType = matrix is not null ? request.IssueType?.Trim() : null,
            IssueCategory = matrix is not null ? request.IssueCategory?.Trim() : null,
            IssueSubCategory = matrix is not null ? request.IssueSubCategory?.Trim() : null,
            AttachmentsJson = stored.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(stored) : null,
            IpAddress = ClientIp,
            UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua
                ? ua[..Math.Min(ua.Length, 500)]
                : null
        };

        // An agency with a helpdesk gets a ticket; every other agency gets mail. The
        // enquiry is saved first either way, so neither route can lose it.
        var connection = await helpdesk.ConnectionForAsync(agency, ct);
        if (connection is not null)
        {
            message.HelpdeskStatus = HelpdeskStatus.Pending;
            // Far enough ahead that the retry sweep leaves it alone while the attempt
            // below is under way.
            message.HelpdeskNextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(3);
        }

        Db.ContactMessages.Add(message);
        await Db.SaveChangesAsync(ct);

        if (connection is not null)
        {
            // Tried now, so the sender can be given the ticket number. Not tied to the
            // request: someone closing the tab should not abandon a ticket half-raised.
            // If it does not go through in time the sweep takes over within minutes.
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            try
            {
                await helpdesk.DispatchAsync(message.Id, budget.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Enquiry {Id} was not raised in the helpdesk in time; the retry sweep will", message.Id);
            }

            await Db.Entry(message).ReloadAsync(CancellationToken.None);
        }
        else
        {
            await NotifyByMailAsync(message, ct);
        }

        logger.LogInformation("Recorded enquiry {Id} from {Email}", message.Id, message.Email);

        if (message.HelpdeskStatus == HelpdeskStatus.Sent && !string.IsNullOrWhiteSpace(message.HelpdeskTicketNumber))
        {
            return Accepted(new
            {
                message = $"Thank you. Your enquiry has been registered with {agency} as ticket " +
                          $"{message.HelpdeskTicketNumber}. Please quote it in any correspondence.",
                reference = message.HelpdeskTicketNumber,
            });
        }

        return Accepted(new
        {
            message = "Thank you. Your enquiry has been received and our team will respond shortly.",
            reference = EnquiryMail.Reference(message),
        });
    }

    /// <summary>
    /// Mails the enquiry to the agency the sender chose. Their own inbox first, the
    /// address they publish next, and the ministry's only if the agency has set
    /// neither - an enquiry that reaches nobody is the one failure this form cannot
    /// have.
    /// </summary>
    private async Task NotifyByMailAsync(ContactMessage message, CancellationToken ct)
    {
        var inbox = await router.ResolveAsync(message.Agency, ct);

        if (string.IsNullOrWhiteSpace(inbox))
        {
            logger.LogWarning(
                "Enquiry {Id} for {Agency} has no inbox configured; it is recorded but nobody was told",
                message.Id, message.Agency ?? "(none)");
            return;
        }

        try
        {
            // Through the agency's own mail server when it has one, the portal's otherwise.
            await email.SendForAgencyAsync(message.Agency, inbox, EnquiryMail.Subject(message), EnquiryMail.Body(message), ct);
        }
        catch (Exception ex)
        {
            // The enquiry is already saved, so a mail server that is down loses the
            // notification, not the enquiry. Logged loudly because nobody is watching
            // a screen for these any more.
            logger.LogError(ex, "Could not notify {Inbox} of enquiry {Id}", inbox, message.Id);
        }
    }

    /// <summary>The grievance matrix the chosen agency uses, if it uses one.</summary>
    private async Task<GrievanceMatrixDto?> MatrixForAsync(string? agency, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(agency)) return null;

        var json = await Db.HelpdeskConnections.AsNoTracking()
            .Where(c => c.PartnerId != null && c.GrievanceMatrix != null
                        && (c.Partner!.ShortName == agency || c.Partner.Name == agency))
            .Select(c => c.GrievanceMatrix)
            .FirstOrDefaultAsync(ct);

        return GrievanceMatrix.Parse(agency, json);
    }

    /// <summary>Adds an address to the newsletter list. Idempotent for an address already present.</summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
            return Accepted(new { message = "Thank you for subscribing." });

        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
            return BadRequestProblem("Please provide a valid e-mail address.");

        var address = request.Email.Trim().ToLowerInvariant();

        var existing = await Db.Subscribers.FirstOrDefaultAsync(s => s.Email == address, ct);
        if (existing is not null)
        {
            if (existing.UnsubscribedAt is not null)
            {
                existing.UnsubscribedAt = null;
                await Db.SaveChangesAsync(ct);
            }

            return Accepted(new { message = "Thank you for subscribing." });
        }

        Db.Subscribers.Add(new Subscriber
        {
            Email = address,
            Name = request.Name?.Trim(),
            Source = "footer",
            ConfirmationToken = Guid.NewGuid().ToString("N")
        });

        await Db.SaveChangesAsync(ct);
        return Accepted(new { message = "Thank you for subscribing." });
    }
}
