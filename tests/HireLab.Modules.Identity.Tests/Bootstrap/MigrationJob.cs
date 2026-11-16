using HireLab.Modules.Identity.Bootstrap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Npgsql;

namespace HireLab.Modules.Identity.Tests.Bootstrap;

/// <summary>
/// The Identity module as the migration job wires it: <see cref="IdentityModule.AddMigrationServices"/> with the given
/// settings and environment, on the test database and the test host's clock, logging to a <see cref="FakeLogCollector"/>.
/// </summary>
internal sealed class MigrationJob : IAsyncDisposable
{
    public const string PublicBaseUrl = "https://hirelab.example.com";
    public const string AdminEmail = "first.admin@example.com";

    private readonly ServiceProvider _services;

    private MigrationJob(ServiceProvider services)
    {
        _services = services;
    }

    public FakeLogCollector Logs => _services.GetRequiredService<FakeLogCollector>();

    public static MigrationJob Create(
        HireLabTestHost host,
        IReadOnlyDictionary<string, string?>? settings = null,
        string environment = "Production")
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? DefaultSettings())
            .Build());
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = environment });
        services.AddSingleton<TimeProvider>(host.Time);
        services.AddSingleton(_ => NpgsqlDataSource.Create(host.DatabaseConnectionString));
        services.AddFakeLogging();

        new IdentityModule().AddMigrationServices(services);

        return new MigrationJob(services.BuildServiceProvider());
    }

    public static Dictionary<string, string?> DefaultSettings(bool issueNewLink = false, string adminEmail = AdminEmail) => new()
    {
        ["HireLab:PublicBaseUrl"] = PublicBaseUrl,
        ["AdminBootstrap:Email"] = adminEmail,
        [IdentityModule.IssueAdminLinkSetting] = issueNewLink ? "true" : "false",
    };

    public static async Task<string?> RunAsync(HireLabTestHost host, IReadOnlyDictionary<string, string?>? settings = null)
    {
        await using var job = Create(host, settings);
        await job.BootstrapAsync();
        return job.AdminInviteLink();
    }

    public async Task BootstrapAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        await new IdentityModule().BootstrapAsync(scope.ServiceProvider, TestContext.Current.CancellationToken);
    }

    /// <summary>The link the run wrote to its log, or <c>null</c> when it wrote none.</summary>
    public string? AdminInviteLink() => Logs.GetSnapshot()
        .Where(record => record.Category == typeof(AdminBootstrap).FullName && record.Level == LogLevel.Warning)
        .Select(record => record.StructuredState?.SingleOrDefault(pair => pair.Key == "AdminInviteLink").Value)
        .SingleOrDefault();

    public T GetRequiredService<T>()
        where T : notnull => _services.GetRequiredService<T>();

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
