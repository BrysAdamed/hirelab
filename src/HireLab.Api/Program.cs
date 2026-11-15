using System.Text.Json.Serialization;
using HireLab.Api;
using HireLab.Api.Auth;
using HireLab.Api.Infrastructure;
using HireLab.Api.Modules;
using HireLab.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureNpgsqlDataSource(ResourceNames.Database);
builder.AddAzureBlobServiceClient(ResourceNames.Blobs, settings => settings.DisableHealthChecks = true);

builder.AddHireLabDataProtection();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddStaffAndCandidateAuth();

builder.AddModules(ModuleCatalog.All);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapModules();
app.MapSpaHosting();

await app.RunAsync();
