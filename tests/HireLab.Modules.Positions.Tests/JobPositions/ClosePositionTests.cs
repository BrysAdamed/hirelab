using System.Net;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Tests.Attachments;
using HireLab.Modules.Positions.Tests.Candidates;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.JobPositions;

public sealed class ClosePositionTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Manager_closes_a_position_and_every_candidate_is_anonymised()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var first = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake First"));
        var second = await Host.AddCandidateAsync(positionId, new CandidateBuilder().Anonymised());
        using var manager = await CreateBrowserAsync(managerStaff);
        await manager.UploadedAsync(first, "cv.pdf", FakeFiles.Pdf);
        await manager.UploadedAsync(first, "notes.txt", FakeFiles.Text("Fake notes"));
        var secondBefore = await manager.CandidateAsync(second);
        var version = (await manager.PositionAsync(positionId)).Version;

        // Act
        using var response = await manager.CloseAsync(positionId, version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var position = await manager.PositionAsync(positionId);
        position.State.ShouldBe(JobPositionState.Closed);
        position.ClosedAt.ShouldBe(Host.Time.GetUtcNow());
        position.ClosedById.ShouldBe(managerStaff.User.Id.Value);
        position.ClosedByName.ShouldBe(managerStaff.User.DisplayName);

        var candidate = await manager.CandidateAsync(first);
        candidate.DisplayName.ShouldBe("Candidate #1");
        candidate.IsAnonymised.ShouldBeTrue();
        candidate.FullName.ShouldBeNull();
        candidate.Email.ShouldBeNull();
        candidate.Phone.ShouldBeNull();
        candidate.Links.ShouldBeEmpty();
        candidate.AnonymisedAt.ShouldBe(Host.Time.GetUtcNow());
        candidate.AnonymisedByName.ShouldBe(managerStaff.User.DisplayName);
        (await manager.AttachmentsAsync(first)).ShouldBeEmpty();
        (await Host.AttachmentBlobNamesAsync()).ShouldBeEmpty();

        var secondAfter = await manager.CandidateAsync(second);
        secondAfter.AnonymisedById.ShouldBe(secondBefore.AnonymisedById);
        secondAfter.AnonymisedById.ShouldNotBe(managerStaff.User.Id.Value);
    }

    [Fact]
    public async Task After_closing_the_directory_shows_candidates_by_number()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake Directory Person"));
        using var manager = await CreateBrowserAsync(managerStaff);
        using var response = await manager.CloseAsync(positionId, (await manager.PositionAsync(positionId)).Version);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await using var scope = Host.CreateScopeAs(managerStaff.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summary = await directory.FindAsync(candidateId, CancellationToken);

        // Assert
        summary.ShouldBe(new CandidateSummary(candidateId, positionId, "Candidate #1", IsAnonymised: true));
    }

    [Theory]
    [InlineData(StaffRole.Interviewer)]
    [InlineData(StaffRole.Admin)]
    public async Task Only_a_manager_closes_a_position(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var team = staff.User.TeamId ?? (await Host.CreateTeamAsync()).Id;
        var positionId = await Host.CreateJobPositionAsync(team);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.CloseAsync(positionId, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using var teamManager = await ManagerOfAsync(team);
        await ShouldBeOpenWithNamedCandidateAsync(teamManager, positionId, candidateId);
    }

    [Fact]
    public async Task Another_team_position_is_not_found()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var positionId = await Host.CreateJobPositionAsync(otherTeam.Id);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.CloseAsync(positionId, 0);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        using var otherManager = await ManagerOfAsync(otherTeam.Id);
        await ShouldBeOpenWithNamedCandidateAsync(otherManager, positionId, candidateId);
    }

    [Fact]
    public async Task An_already_closed_position_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value, new JobPositionBuilder().Closed());
        using var manager = await CreateBrowserAsync(managerStaff);
        var before = await manager.PositionAsync(positionId);

        // Act
        using var response = await manager.CloseAsync(positionId, before.Version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        (await manager.PositionAsync(positionId)).ShouldBe(before);
    }

    [Fact]
    public async Task A_position_with_an_unfinished_session_is_not_closed()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var teamId = managerStaff.User.TeamId!.Value;
        var positionId = await Host.CreateJobPositionAsync(teamId);
        await Host.AddCandidateAsync(positionId);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var manager = await CreateBrowserAsync(managerStaff);
        var uploaded = await manager.UploadedAsync(candidateId, "cv.pdf", FakeFiles.Pdf);
        Host.UnfinishedSessions.Add(candidateId);

        // Act
        using var response = await manager.CloseAsync(positionId, (await manager.PositionAsync(positionId)).Version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.UnfinishedSessions");
        await ShouldBeOpenWithNamedCandidateAsync(manager, positionId, candidateId);
        (await manager.AttachmentsAsync(candidateId)).ShouldHaveSingleItem().Id.ShouldBe(uploaded.Id);
        (await Host.AttachmentBlobNamesAsync()).ShouldBe([$"{candidateId}/{uploaded.Id}"]);
    }

    [Fact]
    public async Task A_stale_close_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var teamId = managerStaff.User.TeamId!.Value;
        var positionId = await Host.CreateJobPositionAsync(teamId);
        using var manager = await CreateBrowserAsync(managerStaff);
        var staleVersion = (await manager.PositionAsync(positionId)).Version;
        var candidateId = await Host.AddCandidateAsync(positionId);

        // Act
        using var response = await manager.CloseAsync(positionId, staleVersion);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.ChangedMeanwhile");
        await ShouldBeOpenWithNamedCandidateAsync(manager, positionId, candidateId);
    }

    private async Task<BrowserSession> ManagerOfAsync(TeamId teamId)
    {
        var teamManager = await Host.CreateUserAsync(UserBuilder.Manager(teamId));
        return await CreateBrowserAsync(new SignedInStaff(teamManager, await Host.SignInAsync(teamManager)));
    }

    private static async Task ShouldBeOpenWithNamedCandidateAsync(BrowserSession browser, JobPositionId positionId, CandidateId candidateId)
    {
        var position = await browser.PositionAsync(positionId);
        position.State.ShouldBe(JobPositionState.Open);
        position.ClosedAt.ShouldBeNull();
        position.ClosedById.ShouldBeNull();
        var candidate = await browser.CandidateAsync(candidateId);
        candidate.IsAnonymised.ShouldBeFalse();
        candidate.FullName.ShouldNotBeNull();
        candidate.Email.ShouldNotBeNull();
    }
}
