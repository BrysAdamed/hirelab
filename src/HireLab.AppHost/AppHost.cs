var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("hirelab");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite.WithDataVolume());

var blobs = storage.AddBlobs("blobs");

var migrations = builder.AddProject<Projects.HireLab_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.HireLab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrations)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();
