using HireLab.Api.Auth;
using HireLab.SharedKernel.Modules;

namespace HireLab.Api.Modules;

internal static class ModuleWiring
{
    public const string StaffDocument = "staff";
    public const string CandidateDocument = "candidate";

    public static WebApplicationBuilder AddModules(this WebApplicationBuilder builder, IReadOnlyList<IModule> modules)
    {
        foreach (var module in modules)
        {
            module.AddServices(builder);
            builder.Services.AddSingleton(module);
        }

        builder.Services.AddOpenApi(StaffDocument, options =>
            options.ShouldInclude = description => description.GroupName == StaffDocument);
        builder.Services.AddOpenApi(CandidateDocument, options =>
            options.ShouldInclude = description => description.GroupName == CandidateDocument);

        return builder;
    }

    public static WebApplication MapModules(this WebApplication app)
    {
        var modules = app.Services.GetServices<IModule>();

        var staff = app.MapGroup("/api/staff")
            .RequireAuthorization(AuthPolicies.Staff)
            .WithGroupName(StaffDocument);

        var candidate = app.MapGroup("/api/candidate")
            .RequireAuthorization(AuthPolicies.Candidate)
            .WithGroupName(CandidateDocument);

        foreach (var module in modules)
        {
            module.MapStaffEndpoints(staff);
            module.MapCandidateEndpoints(candidate);
        }

        MapUnknownRouteFallback(staff);
        MapUnknownRouteFallback(candidate);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        return app;
    }

    private static void MapUnknownRouteFallback(RouteGroupBuilder group) =>
        group.Map("/{**path}", () => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound))
            .WithOrder(int.MaxValue)
            .ExcludeFromDescription();
}
