using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>
/// Retries enquiries that could not be raised in the helpdesk first time.
///
/// The form tries straight away, so this only ever sees the ones that failed - a
/// Zoho outage, a token being replaced. Each enquiry is claimed with a single
/// conditional update before it is sent, so if the site ever runs on two servers,
/// or an administrator presses "retry now" as this sweeps, the same enquiry is not
/// raised twice.
/// </summary>
public class HelpdeskRetryService(IServiceScopeFactory scopes, ILogger<HelpdeskRetryService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    /// <summary>How long a claim holds before another sweep may take the enquiry.</summary>
    private static readonly TimeSpan ClaimFor = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the application finish starting - migrations run at start-up.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // A sweep that throws must not stop the next one.
                logger.LogError(ex, "Helpdesk retry sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> SweepAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IHelpdeskDispatcher>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.ContactMessages.AsNoTracking()
            .Where(m => m.HelpdeskStatus == HelpdeskStatus.Pending && m.HelpdeskNextAttemptAt <= now)
            .OrderBy(m => m.HelpdeskNextAttemptAt)
            .Select(m => m.Id)
            .Take(20)
            .ToListAsync(ct);

        var sent = 0;
        foreach (var id in due)
        {
            var claimedUntil = DateTimeOffset.UtcNow + ClaimFor;
            var claimed = await db.ContactMessages
                .Where(m => m.Id == id && m.HelpdeskStatus == HelpdeskStatus.Pending && m.HelpdeskNextAttemptAt <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.HelpdeskNextAttemptAt, claimedUntil), ct);

            if (claimed == 0) continue;

            db.ChangeTracker.Clear();
            if (await dispatcher.DispatchAsync(id, ct) == HelpdeskStatus.Sent) sent++;
        }

        if (due.Count > 0)
            logger.LogInformation("Helpdesk sweep: {Sent} of {Due} waiting enquiries raised", sent, due.Count);

        return sent;
    }
}
