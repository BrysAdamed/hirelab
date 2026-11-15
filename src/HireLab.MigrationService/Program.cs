using HireLab.MigrationService;
using HireLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureNpgsqlDataSource(ResourceNames.Database);

foreach (var migrator in ModuleMigrators.All)
{
    migrator.AddMigrationServices(builder.Services);
}

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.Services.AddHostedService<MigrationWorker>();

await builder.Build().RunAsync();
