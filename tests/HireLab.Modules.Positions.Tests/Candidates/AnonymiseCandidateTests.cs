using System.Net;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Tests.Attachments;
using HireLab.Modules.Positions.Tests.JobPositions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.Candidates;

public sealed class AnonymiseCandidateTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Manager_anonymises_one_candidate_and_their_attachments_are_deleted()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var other = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake Stays"));
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake Withdraws"));
        using var manager = await CreateBrowserAsync(managerStaff);
        await manager.UploadedAsync(candidateId, "cv.pdf", FakeFiles.Pdf);
        await manager.UploadedAsync(candidateId, "photo.png", FakeFiles.Png);
        var kept = await manager.UploadedAsync(other, "cv.pdf", FakeFiles.Pdf);
        var version = (await manager.CandidateAsync(candidateId)).Version;

        // Act
        using var response = await manager.AnonymiseAsync(candidateId, version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var candidate = await manager.CandidateAsync(candidateId);
        candidate.DisplayName.ShouldBe("Candidate #2");
        candidate.IsAnonymised.ShouldBeTrue();
        candidate.FullName.ShouldBeNull();
        candidate.Email.ShouldBeNull();
        candidate.Phone.ShouldBeNull();
        candidate.Links.ShouldBeEmpty();
        candidate.AnonymisedAt.ShouldBe(Host.Time.GetUtcNow());
        candidate.AnonymisedById.ShouldBe(managerStaff.User.Id.Value);
        candidate.AnonymisedByName.ShouldBe(managerStaff.User.DisplayName);
        candidate.PositionState.ShouldBe(JobPositionState.Open);
        (await manager.AttachmentsAsync(candidateId)).ShouldBeEmpty();

        (await manager.CandidateAsync(other)).FullName.ShouldBe("Fake Stays");
        (await manager.AttachmentsAsync(other)).ShouldHaveSingleItem().Id.ShouldBe(kept.Id);
        (await Host.AttachmentBlobNamesAsync()).ShouldBe([$"{other}/{kept.Id}"]);
    }

    [Fact]
    public async Task After_anonymising_the_directory_shows_the_candidate_by_number()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake Directory Person"));
        using var manager = await CreateBrowserAsync(managerStaff);
        using var response = await manager.AnonymiseAsync(candidateId, (await manager.CandidateAsync(candidateId)).Version);
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
    public async Task Only_a_manager_anonymises_a_candidate(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var teamId = staff.User.TeamId ?? (await Host.CreateTeamAsync()).Id;
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(teamId));
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.AnonymiseAsync(candidateId, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using var teamManager = await ManagerOfAsync(teamId);
        (await teamManager.CandidateAsync(candidateId)).IsAnonymised.ShouldBeFalse();
    }

    [Fact]
    public async Task Another_team_candidate_is_not_found()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(otherTeam.Id));
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.AnonymiseAsync(candidateId, 0);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        using var otherManager = await ManagerOfAsync(otherTeam.Id);
        (await otherManager.CandidateAsync(candidateId)).IsAnonymised.ShouldBeFalse();
    }

    [Fact]
    public async Task An_already_anonymised_candidate_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().Anonymised());
        using var manager = await CreateBrowserAsync(managerStaff);
        var before = await manager.CandidateAsync(candidateId);

        // Act
        using var response = await manager.AnonymiseAsync(candidateId, before.Version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Candidate.Anonymised");
        (await manager.CandidateAsync(candidateId)).ShouldBeEquivalentTo(before);
    }

    [Fact]
    public async Task A_candidate_of_a_closed_position_is_already_anonymised()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId);
        await Host.ClosePositionAsync(positionId);
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        using var response = await manager.AnonymiseAsync(candidateId, (await manager.CandidateAsync(candidateId)).Version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Candidate.Anonymised");
    }

    [Fact]
    public async Task A_candidate_with_an_unfinished_session_is_not_anonymised()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value));
        using var manager = await CreateBrowserAsync(managerStaff);
        var uploaded = await manager.UploadedAsync(candidateId, "cv.pdf", FakeFiles.Pdf);
        Host.UnfinishedSessions.Add(candidateId);

        // Act
        using var response = await manager.AnonymiseAsync(candidateId, (await manager.CandidateAsync(candidateId)).Version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Candidate.UnfinishedSessions");
        var candidate = await manager.CandidateAsync(candidateId);
        candidate.IsAnonymised.ShouldBeFalse();
        candidate.FullName.ShouldNotBeNull();
        (await manager.AttachmentsAsync(candidateId)).ShouldHaveSingleItem().Id.ShouldBe(uploaded.Id);
        (await Host.AttachmentBlobNamesAsync()).ShouldBe([$"{candidateId}/{uploaded.Id}"]);
    }

    [Fact]
    public async Task A_stale_anonymise_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value));
        using var manager = await CreateBrowserAsync(managerStaff);
        var staleVersion = (await manager.CandidateAsync(candidateId)).Version;
        using var edited = await manager.EditAsync(candidateId, CandidateApi.ValidInput("Fake Edited Meanwhile"), staleVersion);
        edited.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        using var response = await manager.AnonymiseAsync(candidateId, staleVersion);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.ChangedMeanwhile");
        (await manager.CandidateAsync(candidateId)).FullName.ShouldBe("Fake Edited Meanwhile");
    }

    private async Task<BrowserSession> ManagerOfAsync(TeamId teamId)
    {
        var teamManager = await Host.CreateUserAsync(UserBuilder.Manager(teamId));
        return await CreateBrowserAsync(new SignedInStaff(teamManager, await Host.SignInAsync(teamManager)));
    }
}
