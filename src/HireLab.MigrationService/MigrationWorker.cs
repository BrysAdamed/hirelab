using System.Diagnostics;

namespace HireLab.MigrationService;

internal sealed partial class MigrationWorker(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger)
    : BackgroundService
{
    public const string ActivitySourceName = "HireLab.MigrationService";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity("Migrate database");

        try
        {
            foreach (var migrator in ModuleMigrators.All)
            {
                LogMigrating(migrator.GetType().Name);
                await RunInScopeAsync(migrator.MigrateAsync, stoppingToken);
            }

            if (environment.IsDevelopment())
            {
                foreach (var migrator in ModuleMigrators.All)
                {
                    LogSeeding(migrator.GetType().Name);
                    await RunInScopeAsync(migrator.SeedDevelopmentDataAsync, stoppingToken);
                }
            }

            foreach (var migrator in ModuleMigrators.All)
            {
                LogBootstrapping(migrator.GetType().Name);
                await RunInScopeAsync(migrator.BootstrapAsync, stoppingToken);
            }

            LogCompleted();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error);
            LogFailed(exception);
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task RunInScopeAsync(Func<IServiceProvider, CancellationToken, Task> step, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await step(scope.ServiceProvider, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying migrations of {Module}")]
    private partial void LogMigrating(string module);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding Development data of {Module}")]
    private partial void LogSeeding(string module);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bootstrapping {Module}")]
    private partial void LogBootstrapping(string module);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migration completed")]
    private partial void LogCompleted();

    [LoggerMessage(Level = LogLevel.Critical, Message = "Database migration or bootstrap failed")]
    private partial void LogFailed(Exception exception);
}
