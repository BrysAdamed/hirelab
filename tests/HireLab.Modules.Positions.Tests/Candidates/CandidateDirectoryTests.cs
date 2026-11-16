using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Tests.JobPositions;
using HireLab.SharedKernel.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.Candidates;

public sealed class CandidateDirectoryTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Finds_a_candidate_summary_by_id()
    {
        // Arrange
        var interviewer = await SignInAsAsync(StaffRole.Interviewer);
        var positionId = await Host.CreateJobPositionAsync(interviewer.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Jane Fakeperson"));
        await using var scope = Host.CreateScopeAs(interviewer.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summary = await directory.FindAsync(candidateId, CancellationToken);

        // Assert
        summary.ShouldBe(new CandidateSummary(candidateId, positionId, "Jane Fakeperson", IsAnonymised: false, IsJobPositionClosed: false));
    }

    [Fact]
    public async Task An_anonymised_candidate_is_summarised_by_their_number()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(manager.User.TeamId!.Value);
        await Host.AddCandidateAsync(positionId);
        await Host.AddCandidateAsync(positionId);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().Anonymised());
        await using var scope = Host.CreateScopeAs(manager.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summary = await directory.FindAsync(candidateId, CancellationToken);

        // Assert
        summary.ShouldBe(new CandidateSummary(candidateId, positionId, "Candidate #3", IsAnonymised: true, IsJobPositionClosed: false));
    }

    [Fact]
    public async Task A_candidate_of_a_closed_position_says_so()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(manager.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId);
        await Host.ClosePositionAsync(positionId);
        await using var scope = Host.CreateScopeAs(manager.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summaries = await directory.FindManyAsync([candidateId], CancellationToken);

        // Assert
        summaries[candidateId].IsJobPositionClosed.ShouldBeTrue();
    }

    [Fact]
    public async Task Finds_many_candidates_and_skips_unknown_and_other_team_ids()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        var firstPosition = await Host.CreateJobPositionAsync(teamId);
        var secondPosition = await Host.CreateJobPositionAsync(teamId);
        var first = await Host.AddCandidateAsync(firstPosition, new CandidateBuilder().WithFullName("Fake First"));
        var second = await Host.AddCandidateAsync(secondPosition, new CandidateBuilder().WithFullName("Fake Second"));
        var otherTeam = await Host.CreateTeamAsync();
        var otherTeamCandidate = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(otherTeam.Id));
        await using var scope = Host.CreateScopeAs(manager.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summaries = await directory.FindManyAsync([first, second, otherTeamCandidate, CandidateId.New()], CancellationToken);

        // Assert
        summaries.Count.ShouldBe(2);
        summaries[first].ShouldBe(new CandidateSummary(first, firstPosition, "Fake First", IsAnonymised: false, IsJobPositionClosed: false));
        summaries[second].ShouldBe(new CandidateSummary(second, secondPosition, "Fake Second", IsAnonymised: false, IsJobPositionClosed: false));
        (await directory.FindManyAsync([], CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Another_team_candidate_is_not_found()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(otherTeam.Id));
        var manager = await SignInAsAsync(StaffRole.Manager);
        await using var scope = Host.CreateScopeAs(manager.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summary = await directory.FindAsync(candidateId, CancellationToken);

        // Assert
        summary.ShouldBeNull();
    }

    [Fact]
    public async Task The_admin_finds_no_candidates()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(team.Id));
        var admin = await SignInAsAsync(StaffRole.Admin);
        await using var scope = Host.CreateScopeAs(admin.User);
        var directory = scope.Services.GetRequiredService<ICandidateDirectory>();

        // Act
        var summary = await directory.FindAsync(candidateId, CancellationToken);

        // Assert
        summary.ShouldBeNull();
    }
}
