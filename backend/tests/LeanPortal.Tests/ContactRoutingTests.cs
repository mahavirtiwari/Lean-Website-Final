using System.Text.Json;
using System.Text.Json.Nodes;
using LeanPortal.Api.Controllers.Public;
using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services;
using LeanPortal.Infrastructure.Services.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeanPortal.Tests;

/// <summary>
/// The contact form's routing: QCI's enquiries become Zoho Desk tickets, NPC's go
/// by mail, and neither is ever lost.
///
/// The form sits behind a captcha that only a person can read, so it cannot be
/// driven from a script - these call the controller directly, with a captcha that
/// always passes and a Zoho that records what it was sent.
/// </summary>
public class ContactRoutingTests
{
    // ------------------------------------------------------------------ fakes ----

    private sealed class PassingCaptcha : ICaptchaService
    {
        public CaptchaChallenge Issue() => new("id", "<svg/>");
        public CaptchaChallenge IssueQuestion() => new("id", null, "What is 2 plus 2?");
        public bool Validate(string? id, string? answer) => true;
    }

    private sealed class RecordingMail : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }

        /// <summary>Which agency's server each message would have left through.</summary>
        public List<string?> Via { get; } = [];

        public Task SendForAgencyAsync(string? agency, string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Via.Add(agency);
            return SendAsync(to, subject, htmlBody, ct);
        }

        public Task SendViaAsync(MailRelaySettings relay, string to, string subject, string htmlBody, CancellationToken ct = default) =>
            SendAsync(to, subject, htmlBody, ct);
    }

    private sealed class FakeZoho : IZohoDeskClient
    {
        public bool Fail { get; set; }
        public List<JsonObject> Tickets { get; } = [];
        public List<string> Uploads { get; } = [];

        public Task<string> FindOrCreateContactAsync(ZohoAccount account, ZohoContact contact, CancellationToken ct) =>
            Fail ? throw new ZohoDeskException("Zoho is down") : Task.FromResult("CONTACT-1");

        public Task<string> UploadAsync(ZohoAccount account, Stream content, string fileName, CancellationToken ct)
        {
            Uploads.Add(fileName);
            return Task.FromResult("UPLOAD-" + Uploads.Count);
        }

        public Task<ZohoTicket> CreateTicketAsync(ZohoAccount account, JsonObject ticket, CancellationToken ct)
        {
            Tickets.Add(ticket);
            return Task.FromResult(new ZohoTicket("T-1", "LEAN-0001-110926"));
        }

        public Task<string> CheckAsync(ZohoAccount account, string departmentId, CancellationToken ct) =>
            Task.FromResult("LEAN");
    }

    private sealed class PlainSecrets : ISecretProtector
    {
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? stored) => stored;
        public bool IsProtected(string? value) => false;
    }

    private sealed class MemoryStorage : IFileStorage
    {
        public Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, string folder,
            CancellationToken ct = default) =>
            Task.FromResult(new StoredFile($"/uploads/{folder}/{originalFileName}", originalFileName, content.Length, contentType));

        public Task<bool> DeleteAsync(string url, CancellationToken ct = default) => Task.FromResult(true);
        public bool IsAllowed(string fileName, string contentType, out string? reason) { reason = null; return true; }
        public Stream? OpenRead(string url) => new MemoryStream([1, 2, 3]);
    }

    private sealed class StubSettings(Dictionary<string, string?>? values = null) : ISiteSettingsProvider
    {
        private readonly Dictionary<string, string?> _values = values ?? [];

        public Task<IReadOnlyDictionary<string, string?>> GetPublicSettingsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string?>>(_values);
        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_values.GetValueOrDefault(key));
        public Task<bool> IsEnabledAsync(string key, CancellationToken ct = default, bool fallback = true) =>
            Task.FromResult(bool.TryParse(_values.GetValueOrDefault(key), out var on) ? on : fallback);
        public void Invalidate() { }
    }

    // ---------------------------------------------------------------- harness ----

    private sealed class Harness
    {
        public required ApplicationDbContext Db { get; init; }
        public required ContactController Controller { get; init; }
        public required HelpdeskDispatcher Dispatcher { get; init; }
        public required FakeZoho Zoho { get; init; }
        public required RecordingMail Mail { get; init; }
    }

    private static async Task<Harness> CreateAsync(
        bool helpdeskEnabled = true, bool alsoEmail = false,
        Dictionary<string, string?>? settings = null, bool npcActive = true)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"contact-routing-{Guid.NewGuid()}").Options);

        var qci = new Partner
        {
            Type = PartnerType.ImplementationAgency, Name = "Quality Council of India", ShortName = "QCI",
            EnquiryEmail = "lean@qci.example", IsActive = true,
        };
        var npc = new Partner
        {
            Type = PartnerType.ImplementationAgency, Name = "National Productivity Council", ShortName = "NPC",
            EnquiryEmail = "lean@npc.example", IsActive = npcActive,
        };
        db.Partners.AddRange(qci, npc);
        await db.SaveChangesAsync();

        db.HelpdeskConnections.Add(new HelpdeskConnection
        {
            IsEnabled = helpdeskEnabled,
            PartnerId = qci.Id,
            OrganisationId = "ORG", DepartmentId = "DEPT", ClientId = "CID",
            ClientSecret = "secret", RefreshToken = "refresh",
            TicketTemplate = HelpdeskDispatcher.DefaultTicketTemplate,
            GrievanceMatrix = JsonSerializer.Serialize(
                LeanPortal.Infrastructure.Persistence.Seed.DataSeeder.DefaultGrievanceMatrix),
            AlsoSendEmail = alsoEmail,
        });
        await db.SaveChangesAsync();

        var zoho = new FakeZoho();
        var mail = new RecordingMail();
        var siteSettings = new StubSettings(settings);
        var router = new EnquiryRouter(db, siteSettings);
        var storage = new MemoryStorage();

        var dispatcher = new HelpdeskDispatcher(db, zoho, new PlainSecrets(), storage, router, mail,
            NullLogger<HelpdeskDispatcher>.Instance);

        var controller = new ContactController(db, mail, siteSettings, new PassingCaptcha(), router, storage, dispatcher,
            NullLogger<ContactController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var harness = new Harness
        {
            Db = db, Controller = controller, Dispatcher = dispatcher, Zoho = zoho, Mail = mail,
        };

        return harness;
    }

    private static ContactRequest Enquiry(string agency, bool withGrievance = true) => new()
    {
        Name = "Asha Devi Sharma",
        Email = "asha@example.com",
        Phone = "9876543210",
        Subject = "Payment not reflected",
        Message = "I paid the fee on 3 September and the portal still shows it as due.",
        Agency = agency,
        UserType = withGrievance ? "MSME" : null,
        IssueType = withGrievance ? "Query" : null,
        IssueCategory = withGrievance ? "LEAN" : null,
        IssueSubCategory = withGrievance ? "Payment Related" : null,
        CaptchaId = "id",
        CaptchaAnswer = "ok",
    };

    private static string? Reference(ActionResult result) =>
        JsonNode.Parse(JsonSerializer.Serialize(Assert.IsType<AcceptedResult>(result).Value))?["reference"]?.GetValue<string>();

    // ------------------------------------------------------------------ tests ----

    [Fact]
    public async Task A_QCI_enquiry_becomes_a_ticket_and_the_sender_gets_its_number()
    {
        var h = await CreateAsync();

        var result = await h.Controller.Submit(Enquiry("QCI"), null, CancellationToken.None);

        Assert.Equal("LEAN-0001-110926", Reference(result));
        var ticket = Assert.Single(h.Zoho.Tickets);
        Assert.Equal("CONTACT-1", ticket["contactId"]!.GetValue<string>());
        Assert.Equal("Payment Related", ticket["cf"]!["cf_issue_sub_category"]!.GetValue<string>());
        Assert.Empty(h.Mail.Sent);

        var saved = await h.Db.ContactMessages.SingleAsync();
        Assert.Equal(HelpdeskStatus.Sent, saved.HelpdeskStatus);
        Assert.Equal("LEAN-0001-110926", saved.HelpdeskTicketNumber);
    }

    [Fact]
    public async Task An_NPC_enquiry_goes_by_mail_and_never_near_Zoho()
    {
        var h = await CreateAsync();

        var result = await h.Controller.Submit(Enquiry("NPC", withGrievance: false), null, CancellationToken.None);

        Assert.StartsWith("LEAN-ENQ-", Reference(result));
        Assert.Empty(h.Zoho.Tickets);
        var mail = Assert.Single(h.Mail.Sent);
        Assert.Equal("lean@npc.example", mail.To);
        Assert.Equal(HelpdeskStatus.NotApplicable, (await h.Db.ContactMessages.SingleAsync()).HelpdeskStatus);
        // Sent for NPC, so NPC's own mail server is used when it has one.
        Assert.Equal("NPC", Assert.Single(h.Mail.Via));
    }

    [Fact]
    public async Task A_QCI_enquiry_without_the_grievance_levels_is_refused_and_nothing_is_kept()
    {
        var h = await CreateAsync();

        var result = await h.Controller.Submit(Enquiry("QCI", withGrievance: false), null, CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, ((ObjectResult)result).StatusCode);
        Assert.Empty(await h.Db.ContactMessages.ToListAsync());
    }

    [Fact]
    public async Task With_the_helpdesk_switched_off_QCI_is_mailed_but_still_classified()
    {
        var h = await CreateAsync(helpdeskEnabled: false);

        await h.Controller.Submit(Enquiry("QCI"), null, CancellationToken.None);

        Assert.Empty(h.Zoho.Tickets);
        var mail = Assert.Single(h.Mail.Sent);
        Assert.Equal("lean@qci.example", mail.To);
        Assert.Contains("MSME › Query › LEAN › Payment Related", mail.Body);
    }

    [Fact]
    public async Task When_Zoho_is_down_the_enquiry_waits_for_the_retry_and_is_not_mailed_yet()
    {
        var h = await CreateAsync();
        h.Zoho.Fail = true;

        var result = await h.Controller.Submit(Enquiry("QCI"), null, CancellationToken.None);

        Assert.StartsWith("LEAN-ENQ-", Reference(result));
        var saved = await h.Db.ContactMessages.SingleAsync();
        Assert.Equal(HelpdeskStatus.Pending, saved.HelpdeskStatus);
        Assert.Equal(1, saved.HelpdeskAttempts);
        Assert.NotNull(saved.HelpdeskNextAttemptAt);
        Assert.Contains("Zoho is down", saved.HelpdeskLastError);
        Assert.Empty(h.Mail.Sent);
    }

    [Fact]
    public async Task When_the_retries_run_out_the_agency_is_mailed_instead()
    {
        var h = await CreateAsync();
        h.Zoho.Fail = true;
        await h.Controller.Submit(Enquiry("QCI"), null, CancellationToken.None);

        var saved = await h.Db.ContactMessages.SingleAsync();
        for (var i = 0; i < HelpdeskDispatcher.RetrySchedule.Length; i++)
            await h.Dispatcher.DispatchAsync(saved.Id, CancellationToken.None);

        await h.Db.Entry(saved).ReloadAsync();
        Assert.Equal(HelpdeskStatus.Failed, saved.HelpdeskStatus);
        var mail = Assert.Single(h.Mail.Sent);
        Assert.Equal("lean@qci.example", mail.To);
        Assert.StartsWith("[LEAN Portal] ACTION NEEDED", mail.Subject);
    }

    [Fact]
    public async Task Attachments_are_uploaded_to_Zoho_and_put_on_the_ticket()
    {
        var h = await CreateAsync();
        var files = new FormFileCollection
        {
            new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "attachments", "invoice.pdf")
            {
                Headers = new HeaderDictionary(), ContentType = "application/pdf",
            },
        };

        await h.Controller.Submit(Enquiry("QCI"), files, CancellationToken.None);

        Assert.Equal(["invoice.pdf"], h.Zoho.Uploads);
        Assert.Equal("UPLOAD-1", Assert.Single(h.Zoho.Tickets)["uploads"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task The_form_is_told_which_agencies_use_a_matrix()
    {
        var h = await CreateAsync();

        var options = Assert.IsType<OkObjectResult>((await h.Controller.Options(CancellationToken.None)).Result);
        var matrix = Assert.Single(Assert.IsType<ContactOptionsDto>(options.Value).Grievance);

        Assert.Equal("QCI", matrix.Agency);
        Assert.Equal(4, matrix.Labels.Count);
        Assert.Equal("MSME", Assert.Single(matrix.Options).Name);
    }

    // ----------------------------------------------------------- agency choice ----

    [Fact]
    public async Task With_the_question_switched_off_the_named_agency_applies_whatever_was_sent()
    {
        var h = await CreateAsync(settings: new() { ["feature.contactAgencyChoice"] = "false", ["contact.defaultAgency"] = "npc" });

        var options = (ContactOptionsDto)((OkObjectResult)(await h.Controller.Options(CancellationToken.None)).Result!).Value!;
        Assert.False(options.AskAgency);
        Assert.Equal("NPC", options.DefaultAgency);

        // Sent as QCI, with QCI's grievance levels: the console's choice wins.
        await h.Controller.Submit(Enquiry("QCI"), null, CancellationToken.None);

        Assert.Empty(h.Zoho.Tickets);
        Assert.Equal("lean@npc.example", Assert.Single(h.Mail.Sent).To);
        Assert.Equal("NPC", (await h.Db.ContactMessages.SingleAsync()).Agency);
    }

    [Fact]
    public async Task With_one_agency_left_the_question_is_not_asked_and_it_gets_the_enquiry()
    {
        var h = await CreateAsync(npcActive: false);

        var options = (ContactOptionsDto)((OkObjectResult)(await h.Controller.Options(CancellationToken.None)).Result!).Value!;
        Assert.False(options.AskAgency);
        Assert.Equal("QCI", options.DefaultAgency);

        var result = await h.Controller.Submit(Enquiry("", withGrievance: true), null, CancellationToken.None);

        Assert.Equal("LEAN-0001-110926", Reference(result));
        Assert.Single(h.Zoho.Tickets);
    }

    [Fact]
    public async Task With_the_question_asked_an_agency_that_is_not_offered_is_refused()
    {
        var h = await CreateAsync();

        var result = await h.Controller.Submit(Enquiry("Some Other Body", withGrievance: false), null, CancellationToken.None);

        Assert.Equal(400, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Empty(await h.Db.ContactMessages.ToListAsync());
    }
}
