using System.Text.Json.Serialization;
using HireLab.Api;
using HireLab.Api.Auth;
using HireLab.Api.Infrastructure;
using HireLab.Api.Modules;
using HireLab.Api.Realtime;
using HireLab.ServiceDefaults;
using HireLab.SharedKernel.Users;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddHireLabDatabase(ResourceNames.Database);
builder.AddAzureBlobServiceClient(ResourceNames.Blobs, settings => settings.DisableHealthChecks = true);

builder.AddHireLabDataProtection();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddStaffAndCandidateAuth();
builder.Services.AddApiAntiforgery();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddCurrentUser();
builder.Services.AddRealtime();

builder.AddModules(ModuleCatalog.All);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapModules();
app.MapRealtime();
app.MapSpaHosting();

await app.RunAsync();
