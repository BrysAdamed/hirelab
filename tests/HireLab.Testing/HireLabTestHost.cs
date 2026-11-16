using HireLab.Api;
using HireLab.Api.Modules;
using Azure.Storage.Blobs;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Positions;
using HireLab.Testing.Staff;
using HireLab.SharedKernel.Modules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
using Xunit;

namespace HireLab.Testing;

/// <summary>
/// The shared integration host of a test assembly (ADR 0014): PostgreSQL and Azurite in Testcontainers, every
/// module migrated through its <see cref="IModuleMigrator"/>, and the real API in a
/// <see cref="WebApplicationFactory{TEntryPoint}"/> with a <see cref="FakeTimeProvider"/>.
/// Register it once per assembly with <c>[assembly: AssemblyFixture(typeof(HireLabTestHost))]</c>, disable test
/// parallelisation, and derive test classes from <see cref="IntegrationTest"/>.
/// </summary>
public partial class HireLabTestHost : IAsyncLifetime
{
    /// <summary>The PostgreSQL image, on the same major version as the AppHost.</summary>
    public const string PostgresImage = "postgres:17";

    /// <summary>The Azurite image. Like the AppHost's, it runs with <c>--skipApiVersionCheck</c>, since the Azure SDK is newer.</summary>
    public const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:3.35.0";

    /// <summary>The time every test host's clock starts at.</summary>
    public static readonly DateTimeOffset StartTime = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    private const string MigrationsHistoryTable = "__EFMigrationsHistory";
    private static readonly Uri _baseAddress = new("https://localhost");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImage).WithDatabase(ResourceNames.Database).Build();
    private readonly AzuriteContainer _azurite = new AzuriteBuilder(AzuriteImage).WithCommand("--skipApiVersionCheck").Build();
    private BlobContainerClient? _attachmentBlobs;
    private ApiFactory? _factory;
    private Respawner? _respawner;

    /// <summary>The clock the API uses. It doesn't move unless a test advances it.</summary>
    public FakeTimeProvider Time { get; } = new(StartTime);

    /// <summary>Counts the password hash verifications the API performs.</summary>
    public PasswordHashProbe PasswordHashes { get; } = new();

    /// <summary>Runs a change from another request just before one of the API's saves, to reproduce a lost race.</summary>
    public ConcurrentChange ConcurrentChange { get; } = new();

    /// <summary>The running API's root service provider.</summary>
    public IServiceProvider Services => Factory.Services;

    /// <summary>The connection string of the test database.</summary>
    public string DatabaseConnectionString => _postgres.GetConnectionString();

    /// <summary>The <c>attachments</c> blob container in Azurite, created as the AppHost provisions it. Emptied before every test.</summary>
    public BlobContainerClient AttachmentBlobs =>
        _attachmentBlobs ?? throw new InvalidOperationException("The test host has not been initialised.");

    private ApiFactory Factory => _factory ?? throw new InvalidOperationException("The test host has not been initialised.");

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _azurite.StartAsync());

        _attachmentBlobs = new BlobServiceClient(_azurite.GetConnectionString()).GetBlobContainerClient(PositionsModule.AttachmentsContainerName);
        await _attachmentBlobs.CreateIfNotExistsAsync();

        await MigrateAsync();
        await CreateTestTablesAsync();
        _respawner = await CreateRespawnerAsync();

        _factory = new ApiFactory(this);
        _factory.StartServer();
    }

    /// <summary>
    /// Deletes all data except the migrations history (nothing while no module has tables) and all attachment blobs, and resets <see cref="PasswordHashes"/>.
    /// <see cref="IntegrationTest"/> calls it before every test.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("The test host has not been initialised.");
        }

        PasswordHashes.Reset();
        ConcurrentChange.Reset();

        await foreach (var blob in AttachmentBlobs.GetBlobsAsync())
        {
            await AttachmentBlobs.DeleteBlobIfExistsAsync(blob.Name);
        }

        if (_respawner is not { } respawner)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Creates a client for the API over HTTPS with its own client IP address that doesn't follow redirects or keep
    /// cookies, sending only the given cookies.
    /// </summary>
    public HttpClient CreateClient(IEnumerable<SetCookieHeaderValue>? cookies = null)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = _baseAddress,
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        client.DefaultRequestHeaders.Add(TestClientAddress.HeaderName, TestClientAddress.Next());

        var cookieHeader = string.Join("; ", (cookies ?? []).Select(cookie => $"{cookie.Name}={cookie.Value}"));
        if (cookieHeader.Length > 0)
        {
            client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookieHeader);
        }

        return client;
    }

    /// <summary>
    /// Creates a <see cref="BrowserSession"/> that starts with <paramref name="cookies"/> (for example a signed-in staff
    /// user's) and holds an antiforgery token for that user, so it can call unsafe endpoints.
    /// </summary>
    public async Task<BrowserSession> CreateBrowserAsync(IEnumerable<SetCookieHeaderValue>? cookies = null)
    {
        var browser = CreateBrowser(cookies);
        await browser.RefreshAntiforgeryTokenAsync();
        return browser;
    }

    /// <summary>Creates a <see cref="BrowserSession"/> that starts with <paramref name="cookies"/> and no antiforgery token.</summary>
    public BrowserSession CreateBrowser(IEnumerable<SetCookieHeaderValue>? cookies = null) => new(CreateClient(), cookies ?? []);

    /// <summary>Signs in with the candidate cookie scheme through a test-only endpoint and returns the issued cookies.</summary>
    public Task<IReadOnlyList<SetCookieHeaderValue>> SignInAsCandidateAsync() => SignInAsync(TestSignIn.CandidateUri);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _azurite.DisposeAsync();
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Lets a test assembly add test-only services or endpoints to the API, for example with <c>ConfigureTestServices</c>.</summary>
    protected virtual void ConfigureWebHost(IWebHostBuilder builder)
    {
    }

    /// <summary>
    /// Lets a test assembly create the tables of its test-only DbContexts in <see cref="DatabaseConnectionString"/>.
    /// Runs after the module migrations and before Respawn is set up, so those tables are reset between tests too.
    /// </summary>
    protected virtual Task CreateTestTablesAsync() => Task.CompletedTask;

    private async Task MigrateAsync()
    {
        var migrators = ModuleCatalog.All.OfType<IModuleMigrator>().ToList();

        var services = new ServiceCollection();
        services.AddSingleton(_ => NpgsqlDataSource.Create(DatabaseConnectionString));
        foreach (var migrator in migrators)
        {
            migrator.AddMigrationServices(services);
        }

        await using var provider = services.BuildServiceProvider();
        foreach (var migrator in migrators)
        {
            await using var scope = provider.CreateAsyncScope();
            await migrator.MigrateAsync(scope.ServiceProvider, CancellationToken.None);
        }
    }

    private async Task<Respawner?> CreateRespawnerAsync()
    {
        await using var connection = new NpgsqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        if (!await HasDataTablesAsync(connection))
        {
            return null;
        }

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = [new Table(MigrationsHistoryTable)],
        });
    }

    private static async Task<bool> HasDataTablesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            $"""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_schema NOT IN ('pg_catalog', 'information_schema')
                  AND table_name <> '{MigrationsHistoryTable}')
            """,
            connection);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<IReadOnlyList<SetCookieHeaderValue>> SignInAsync(Uri signInUri)
    {
        using var client = CreateClient();
        using var response = await client.PostAsync(signInUri, content: null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return SetCookieHeaderValue.ParseList(response.Headers.GetValues(HeaderNames.SetCookie).ToList()).ToList();
    }

    private sealed class ApiFactory(HireLabTestHost host) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting($"ConnectionStrings:{ResourceNames.Database}", host.DatabaseConnectionString);
            builder.UseSetting($"ConnectionStrings:{ResourceNames.Blobs}", host._azurite.GetConnectionString());

            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(host.Time));
                services.AddSingleton<IStartupFilter, TestClientAddress>();
                services.AddSingleton<IStartupFilter, TestSignIn>();
                services.AddSingleton(host.PasswordHashes);
                services.Replace(ServiceDescriptor.Scoped<IPasswordHasher<User>, ProbingPasswordHasher>());
                ConcurrentChange.AddToEveryDbContext(services, host.ConcurrentChange);
            });

            host.ConfigureWebHost(builder);
        }
    }
}
