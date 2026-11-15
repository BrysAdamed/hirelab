using HireLab.Api;
using HireLab.Api.Auth;
using HireLab.Api.Infrastructure;
using HireLab.Api.Modules;
using HireLab.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource(ResourceNames.Database);
builder.AddAzureBlobServiceClient(ResourceNames.Blobs, settings => settings.DisableHealthChecks = true);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddStaffAndCandidateAuth();

builder.AddModules(ModuleCatalog.All);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapModules(ModuleCatalog.All);
app.MapSpaHosting();

await app.RunAsync();
