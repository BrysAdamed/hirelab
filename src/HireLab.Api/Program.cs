using HireLab.Api;
using HireLab.Api.Infrastructure;
using HireLab.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource(ResourceNames.Database);
builder.AddAzureBlobServiceClient(ResourceNames.Blobs, settings => settings.DisableHealthChecks = true);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();
await app.RunAsync();
