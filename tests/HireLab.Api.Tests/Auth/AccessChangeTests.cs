using System.Net;
using System.Net.Http.Json;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Api.Tests.Auth;

public sealed class AccessChangeTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly Uri _notes = new(TeamScopeProbeModule.NotesPath, UriKind.Relative);
    private static readonly TimeSpan _pastValidationInterval = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Reassigned_interviewer_no_longer_reads_the_old_teams_data()
    {
        // Arrange
        var oldTeam = await Host.CreateTeamAsync();
        var newTeam = await Host.CreateTeamAsync();
        var oldNote = await AddNoteAsync(oldTeam.Id);
        var newNote = await AddNoteAsync(newTeam.Id);
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(oldTeam.Id));
        using var oldSession = Host.CreateClient(await Host.SignInAsync(interviewer));
        var notesBefore = await oldSession.GetFromJsonAsync<List<Guid>>(_notes, CancellationToken);
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var reassigned = await admin.PostAsJsonAsync(
            new Uri($"/api/staff/users/{interviewer.Id}/reassign", UriKind.Relative),
            new { teamId = newTeam.Id.Value });
        Host.Time.Advance(_pastValidationInterval);
        using var oldSessionAfter = await oldSession.GetAsync(_notes, CancellationToken);
        using var newSession = Host.CreateClient(await Host.SignInAsync(interviewer));
        var notesAfter = await newSession.GetFromJsonAsync<List<Guid>>(_notes, CancellationToken);

        // Assert
        notesBefore.ShouldBe([oldNote]);
        reassigned.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        oldSessionAfter.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        notesAfter.ShouldBe([newNote]);
    }

    [Fact]
    public async Task Interviewer_deactivated_by_their_manager_loses_the_session_at_the_next_validation()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var session = Host.CreateClient(await Host.SignInAsync(interviewer));
        using var managerBrowser = await CreateBrowserAsync(new SignedInStaff(manager, await Host.SignInAsync(manager)));

        // Act
        using var deactivated = await managerBrowser.PostAsync(
            new Uri($"/api/staff/my-team/interviewers/{interviewer.Id}/deactivate", UriKind.Relative));
        Host.Time.Advance(_pastValidationInterval);
        using var after = await session.GetAsync(_notes, CancellationToken);

        // Assert
        deactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        after.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
