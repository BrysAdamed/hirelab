using System.Net;
using System.Net.Http.Json;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Api.Tests.Auth;

public sealed class TeamScopingTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly Uri _notes = new(TeamScopeProbeModule.NotesPath, UriKind.Relative);

    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Team_member_sees_only_their_teams_rows(StaffRole role)
    {
        // Arrange
        var ownTeam = await Host.CreateTeamAsync();
        var otherTeam = await Host.CreateTeamAsync();
        var ownNotes = new[] { await AddNoteAsync(ownTeam.Id), await AddNoteAsync(ownTeam.Id) };
        await AddNoteAsync(otherTeam.Id);
        using var client = await SignInToTeamAsync(role, ownTeam.Id);

        // Act
        var visible = await client.GetFromJsonAsync<List<Guid>>(_notes, CancellationToken);

        // Assert
        visible.ShouldBe(ownNotes, ignoreOrder: true);
    }

    [Fact]
    public async Task Admin_sees_no_team_owned_rows()
    {
        // Arrange
        await AddNoteAsync((await Host.CreateTeamAsync()).Id);
        await AddNoteAsync((await Host.CreateTeamAsync()).Id);
        using var client = CreateClient(await SignInAsAsync(StaffRole.Admin));

        // Act
        var visible = await client.GetFromJsonAsync<List<Guid>>(_notes, CancellationToken);

        // Assert
        visible.ShouldBeEmpty();
    }

    [Fact]
    public async Task Another_teams_row_is_not_found()
    {
        // Arrange
        var ownTeam = await Host.CreateTeamAsync();
        var ownNote = await AddNoteAsync(ownTeam.Id);
        var otherTeamsNote = await AddNoteAsync((await Host.CreateTeamAsync()).Id);
        using var client = await SignInToTeamAsync(StaffRole.Manager, ownTeam.Id);

        // Act
        using var own = await client.GetAsync(TeamScopeProbeModule.NoteUri(ownNote), CancellationToken);
        using var other = await client.GetAsync(TeamScopeProbeModule.NoteUri(otherTeamsNote), CancellationToken);

        // Assert
        own.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        other.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Code_outside_a_request_sees_no_team_owned_rows_unless_it_bypasses_the_filter()
    {
        // Arrange
        await AddNoteAsync((await Host.CreateTeamAsync()).Id);
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamNotesDbContext>();

        // Act
        var filtered = await dbContext.Notes.CountAsync(CancellationToken);
        var unfiltered = await dbContext.Notes.IgnoreQueryFilters([TeamQueryFilter.Name]).CountAsync(CancellationToken);

        // Assert
        filtered.ShouldBe(0);
        unfiltered.ShouldBe(1);
    }

    private async Task<HttpClient> SignInToTeamAsync(StaffRole role, TeamId teamId)
    {
        var builder = role == StaffRole.Manager ? UserBuilder.Manager(teamId) : UserBuilder.Interviewer(teamId);
        var user = await Host.CreateUserAsync(builder);
        return Host.CreateClient(await Host.SignInAsync(user));
    }

    private async Task<Guid> AddNoteAsync(TeamId teamId)
    {
        var note = new TeamNote(Guid.CreateVersion7(), teamId);

        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamNotesDbContext>();
        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(CancellationToken);

        return note.Id;
    }
}
