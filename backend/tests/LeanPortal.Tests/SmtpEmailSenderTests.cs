using System.Net;
using System.Net.Sockets;
using System.Text;
using LeanPortal.Infrastructure.Services;
using LeanPortal.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeanPortal.Tests;

/// <summary>
/// Proves the portal actually puts an enquiry notification on the wire.
///
/// Worth testing rather than assuming: until this was written the only IEmailSender
/// in the codebase logged and discarded, so nothing had ever exercised a send. The
/// enquiry form cannot be driven from a test - it is behind a captcha that is
/// deliberately unreadable - and enquiries are no longer read in the console, so
/// this is the last link in the chain that reaches a person.
///
/// The listener is a few lines of socket code in the test itself rather than a
/// service the suite has to have running, so this passes on a build agent.
/// </summary>
public class SmtpEmailSenderTests
{
    /// <summary>A one-shot SMTP server that records the conversation and hangs up.</summary>
    private sealed class Listener : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task<string> _session;

        public Listener()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _session = Task.Run(AcceptAsync);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public Task<string> Conversation => _session;

        private async Task<string> AcceptAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            var log = new StringBuilder();
            await writer.WriteAsync("220 test\r\n");

            var inData = false;
            while (await reader.ReadLineAsync() is { } line)
            {
                log.AppendLine(line);

                if (inData)
                {
                    if (line == ".")
                    {
                        inData = false;
                        await writer.WriteAsync("250 OK\r\n");
                    }

                    continue;
                }

                var command = line.ToUpperInvariant();
                if (command.StartsWith("EHLO") || command.StartsWith("HELO"))
                    await writer.WriteAsync("250 test\r\n");
                else if (command.StartsWith("DATA"))
                {
                    inData = true;
                    await writer.WriteAsync("354 send it\r\n");
                }
                else if (command.StartsWith("QUIT"))
                {
                    await writer.WriteAsync("221 bye\r\n");
                    break;
                }
                else
                    await writer.WriteAsync("250 OK\r\n");
            }

            return log.ToString();
        }

        public void Dispose() => _listener.Stop();
    }

    /// <summary>Hands the sender a fixed relay, standing in for console or file.</summary>
    private sealed class StubRelay(MailRelaySettings settings) : IMailRelayProvider
    {
        public Task<MailRelaySettings> GetAsync(CancellationToken ct = default) => Task.FromResult(settings);

        public Task<MailRelaySettings> GetForAgencyAsync(string? agency, CancellationToken ct = default) =>
            Task.FromResult(settings);
    }

    private static SmtpEmailSender SenderFor(int port, string? copyTo = null) =>
        new(new StubRelay(new MailRelaySettings(
                Host: "127.0.0.1",
                Port: port,
                Encryption: "None",
                User: null,
                Password: null,
                FromAddress: "no-reply@lean.msme.gov.in",
                FromName: "MSME Competitive (LEAN) Scheme",
                CopyTo: copyTo)),
            NullLogger<SmtpEmailSender>.Instance);

    [Fact]
    public async Task Delivers_the_notification_to_the_agency_inbox()
    {
        using var listener = new Listener();

        await SenderFor(listener.Port).SendAsync(
            "enquiries@qci.example",
            "[LEAN Portal] New enquiry: Subsidy question",
            "<p>A new enquiry has been submitted.</p>");

        var conversation = await listener.Conversation.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains("MAIL FROM:<no-reply@lean.msme.gov.in>", conversation);
        Assert.Contains("RCPT TO:<enquiries@qci.example>", conversation);
        Assert.Contains("Subsidy question", conversation);
        Assert.Contains("A new enquiry has been submitted.", conversation);
    }

    [Fact]
    public async Task Sends_the_body_as_html_so_the_markup_is_not_read_literally()
    {
        using var listener = new Listener();

        await SenderFor(listener.Port).SendAsync("a@b.example", "Subject", "<p>Body</p>");
        var conversation = await listener.Conversation.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains("text/html", conversation);
    }

    [Fact]
    public async Task Blind_copies_the_ministry_when_one_is_configured()
    {
        using var listener = new Listener();

        await SenderFor(listener.Port, copyTo: "scheme-team@msme.example")
            .SendAsync("enquiries@npc.example", "Subject", "<p>Body</p>");

        var conversation = await listener.Conversation.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains("RCPT TO:<enquiries@npc.example>", conversation);
        Assert.Contains("RCPT TO:<scheme-team@msme.example>", conversation);
        // Blind: the copy must not be announced in the headers the recipient reads.
        Assert.DoesNotContain("Bcc:", conversation);
    }

    [Fact]
    public async Task Refuses_to_pretend_when_no_relay_is_configured()
    {
        var sender = new SmtpEmailSender(
            new StubRelay(new MailRelaySettings(null, 587, "StartTls", null, null,
                "no-reply@lean.msme.gov.in", null, null)),
            NullLogger<SmtpEmailSender>.Instance);

        // Throwing beats returning quietly: the caller logs the failure, and an
        // enquiry that was never sent must not look as though it was.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("a@b.example", "Subject", "<p>Body</p>"));
    }

    [Fact]
    public async Task Refuses_when_no_from_address_is_configured()
    {
        var sender = new SmtpEmailSender(
            new StubRelay(new MailRelaySettings("127.0.0.1", 587, "StartTls", null, null,
                null, null, null)),
            NullLogger<SmtpEmailSender>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("a@b.example", "Subject", "<p>Body</p>"));
    }
}
