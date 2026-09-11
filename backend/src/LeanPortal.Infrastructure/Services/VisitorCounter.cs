using LeanPortal.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services;

/// <summary>
/// The site's visitor counter, counted in memory and written to the database in
/// batches.
///
/// <para>
/// Every visitor's first page used to run its own UPDATE on the one row that holds
/// the total. That is correct, but it is also a queue: ten thousand visitors
/// arriving together each wait their turn for the same row lock, each holding a
/// pooled connection while they wait, and the pages behind them wait for a
/// connection. Here a visit is an interlocked add, and one UPDATE every few
/// seconds carries all of them.
/// </para>
/// <para>
/// The UPDATE adds the batch to whatever the row holds, so a figure an editor
/// types into the console is kept, and two servers counting at once cannot
/// overwrite each other. What is lost if the process dies is at most the last few
/// seconds of visits; a normal shutdown writes them first.
/// </para>
/// </summary>
public sealed class VisitorCounter(IServiceScopeFactory scopes, ILogger<VisitorCounter> logger) : BackgroundService
{
    private static readonly TimeSpan FlushEvery = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Visits counted here and not yet written.</summary>
    private long _pending;

    /// <summary>The total the database last reported; -1 until it has been read.</summary>
    private long _stored = -1;

    /// <summary>Counts one visit and returns the total including it.</summary>
    public async Task<long> RecordAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _pending);
        return await StoredAsync(ct) + Interlocked.Read(ref _pending);
    }

    /// <summary>The total, without counting anything.</summary>
    public async Task<long> CurrentAsync(CancellationToken ct) =>
        await StoredAsync(ct) + Interlocked.Read(ref _pending);

    /// <summary>
    /// Writes the visits counted so far and re-reads the total, which also picks up
    /// a figure an editor has changed in the console.
    /// </summary>
    public async Task FlushAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        var batch = Interlocked.Exchange(ref _pending, 0);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var total = batch > 0
                ? await db.IncrementVisitorCountAsync(batch, ct)
                : await db.ReadVisitorCountAsync(ct);

            Interlocked.Exchange(ref _stored, total);
        }
        catch
        {
            // Not written: keep them for the next attempt.
            Interlocked.Add(ref _pending, batch);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushEvery);
        while (await WaitAsync(timer, stoppingToken))
        {
            try
            {
                await FlushAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not write the visitor count; will try again");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        // The visits since the last write, so a recycle does not lose them.
        if (Interlocked.Read(ref _pending) == 0) return;
        try
        {
            await FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not write the visitor count on shutdown");
        }
    }

    private async Task<long> StoredAsync(CancellationToken ct)
    {
        var known = Interlocked.Read(ref _stored);
        if (known >= 0) return known;

        // First visit since start-up: read the total once, not once per visitor.
        await _gate.WaitAsync(ct);
        try
        {
            if (_stored < 0)
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Interlocked.Exchange(ref _stored, await db.ReadVisitorCountAsync(ct));
            }
            return _stored;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
