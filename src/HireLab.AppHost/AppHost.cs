using HireLab.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("hirelab");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite.WithDataVolume());

var blobs = storage.AddBlobs("blobs");

var api = builder.AddProject<Projects.HireLab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithHttpHealthCheck("/health");

var staff = builder.AddAngularDevServer("staff", port: 4200, api);

builder.AddAngularDevServer("candidate", port: 4201, api, basePath: "/s/")
    .WithNpm(install: false)
    .WaitForNpmInstallOf(staff);

await builder.Build().RunAsync();
