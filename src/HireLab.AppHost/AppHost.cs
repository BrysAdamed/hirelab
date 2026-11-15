using HireLab.AppHost;
using HireLab.AppHost.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithHireLabSettings()
    .ClearDefaultRoleAssignments()
    .RunAsContainer(container => container
        .WithImageTag(AzureResourceSettings.PostgresContainerImageTag)
        .WithDataVolume(AzureResourceSettings.PostgresDataVolumeName));

var database = postgres.AddDatabase("hirelab");

var storage = builder.AddAzureStorage("storage")
    .WithHireLabSettings()
    .RunAsEmulator(azurite => azurite.WithDataVolume());

var blobs = storage.AddBlobs("blobs");
var dataProtectionKeys = storage.AddBlobContainer("data-protection-keys");
storage.AddBlobContainer("attachments");

var api = builder.AddProject<Projects.HireLab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

if (builder.ExecutionContext.IsRunMode)
{
    var staff = builder.AddAngularDevServer("staff", port: 4200, api);

    builder.AddAngularDevServer("candidate", port: 4201, api, basePath: "/s/")
        .WithNpm(install: false)
        .WaitForNpmInstallOf(staff);
}
else
{
    // TODO HLB-25: keep the result and call PublishAsMigrationJob(azure) on the migration worker.
    builder.AddAzureProduction(api, postgres, dataProtectionKeys);
}

await builder.Build().RunAsync();
