using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Positions.Attachments.OrphanSweep;

/// <summary>
/// Runs the <see cref="OrphanAttachmentSweeper"/> shortly after the API starts and then every interval (ADR 0027). A
/// failed sweep is logged and the next one runs on schedule.
/// </summary>
internal sealed partial class OrphanAttachmentSweep(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OrphanAttachmentSweepOptions> options,
    ILogger<OrphanAttachmentSweep> logger)
    : BackgroundService
{
    /// <summary>The wait before the first sweep, so it doesn't compete with startup and never runs in a host that only starts briefly.</summary>
    public static readonly TimeSpan FirstSweepDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            LogDisabled();
            return;
        }

        using var timer = new PeriodicTimer(settings.Interval, timeProvider);
        try
        {
            await Task.Delay(FirstSweepDelay, timeProvider, stoppingToken);
            await SweepAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SweepAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task SweepAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<OrphanAttachmentSweeper>().SweepAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            LogSweepFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The orphan attachment sweep is disabled")]
    private partial void LogDisabled();

    [LoggerMessage(Level = LogLevel.Error, Message = "The orphan attachment sweep failed; the next one runs on schedule")]
    private partial void LogSweepFailed(Exception exception);
}
