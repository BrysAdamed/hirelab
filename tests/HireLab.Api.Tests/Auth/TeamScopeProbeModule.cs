using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace HireLab.Api.Tests.Auth;

internal sealed class TeamScopeProbeModule : IModule
{
    public const string NotesPath = "/api/staff/__probe/team-notes";

    private const string NotesPattern = "/__probe/team-notes";

    public static Uri NoteUri(Guid id) => new($"{NotesPath}/{id}", UriKind.Relative);

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapStaffEndpoints(IEndpointRouteBuilder staff)
    {
        staff.MapGet(NotesPattern, async (TeamNotesDbContext dbContext, CancellationToken cancellationToken) =>
            TypedResults.Ok(await dbContext.Notes.Select(note => note.Id).ToListAsync(cancellationToken)));

        staff.MapGet($"{NotesPattern}/{{id:guid}}", async (Guid id, TeamNotesDbContext dbContext, CancellationToken cancellationToken) =>
            await dbContext.Notes.AnyAsync(note => note.Id == id, cancellationToken)
                ? Results.NoContent()
                : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound));
    }

    public void MapCandidateEndpoints(IEndpointRouteBuilder candidate)
    {
    }
}

internal sealed class TeamNote(Guid id, TeamId teamId) : ITeamOwned
{
    public Guid Id { get; private set; } = id;

    public TeamId TeamId { get; private set; } = teamId;
}

internal sealed class TeamNotesDbContext(DbContextOptions<TeamNotesDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "test_team_notes";

    public DbSet<TeamNote> Notes => Set<TeamNote>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.AddSharedKernelIdConversions();
}
