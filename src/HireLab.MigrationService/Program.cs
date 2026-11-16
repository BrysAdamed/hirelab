using HireLab.MigrationService;
using HireLab.ServiceDefaults;
using HireLab.TaskTypes.Catalog;

var builder = Host.CreateApplicationBuilder(CommandLine.ToConfigurationArgs(args));

builder.AddServiceDefaults();
builder.AddHireLabDatabase(ResourceNames.Database);

foreach (var migrator in ModuleMigrators.All)
{
    migrator.AddMigrationServices(builder.Services);
}

builder.Services.AddTaskTypes();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.Services.AddHostedService<MigrationWorker>();

await builder.Build().RunAsync();
