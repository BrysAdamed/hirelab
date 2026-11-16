using System.Net;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Tests.Candidates;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;

namespace HireLab.Modules.Positions.Tests.JobPositions;

public sealed class JobPositionTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Manager_creates_an_open_position_in_their_team()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var input = JobPositionApi.ValidInput();

        // Act
        var positionId = await manager.CreatedAsync(input);

        // Assert
        var position = await manager.PositionAsync(positionId);
        position.ShouldBe(new JobPositionView(
            positionId.Value,
            input.Title!,
            input.Description,
            input.Requirements!,
            input.NiceToHaves,
            input.InterviewerNotes,
            JobPositionState.Open,
            Host.Time.GetUtcNow(),
            ClosedAt: null,
            ClosedById: null,
            ClosedByName: null,
            position.Version));
        (await manager.PositionsAsync()).ShouldHaveSingleItem().Id.ShouldBe(positionId.Value);
    }

    [Fact]
    public async Task Blank_optional_sections_are_stored_as_empty_and_the_title_is_trimmed()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        var positionId = await manager.CreatedAsync(new JobPositionInput("  Fake QA Engineer  ", "   ", "- Tests fake apps", null, ""));

        // Assert
        var position = await manager.PositionAsync(positionId);
        position.Title.ShouldBe("Fake QA Engineer");
        position.Description.ShouldBeNull();
        position.NiceToHaves.ShouldBeNull();
        position.InterviewerNotes.ShouldBeNull();
    }

    [Fact]
    public async Task Interviewer_lists_and_views_their_team_positions()
    {
        // Arrange
        var interviewerStaff = await SignInAsAsync(StaffRole.Interviewer);
        var positionId = await Host.CreateJobPositionAsync(interviewerStaff.User.TeamId!.Value, new JobPositionBuilder().WithTitle("Fake Designer"));
        using var interviewer = await CreateBrowserAsync(interviewerStaff);

        // Act
        var positions = await interviewer.PositionsAsync();
        var position = await interviewer.PositionAsync(positionId);

        // Assert
        positions.ShouldHaveSingleItem().Title.ShouldBe("Fake Designer");
        position.Title.ShouldBe("Fake Designer");
    }

    [Fact]
    public async Task Interviewer_cannot_create_edit_or_delete_positions()
    {
        // Arrange
        var interviewerStaff = await SignInAsAsync(StaffRole.Interviewer);
        var positionId = await Host.CreateJobPositionAsync(interviewerStaff.User.TeamId!.Value);
        using var interviewer = await CreateBrowserAsync(interviewerStaff);
        var version = (await interviewer.PositionAsync(positionId)).Version;

        // Act
        using var created = await interviewer.CreateAsync(JobPositionApi.ValidInput());
        using var edited = await interviewer.EditAsync(positionId, JobPositionApi.ValidInput("Fake Renamed"), version);
        using var deleted = await interviewer.DeleteAsync(JobPositionApi.Position(positionId));

        // Assert
        created.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        edited.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        deleted.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await interviewer.PositionsAsync()).ShouldHaveSingleItem().Title.ShouldNotBe("Fake Renamed");
    }

    [Fact]
    public async Task Admin_cannot_read_positions()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.GetAsync(JobPositionApi.Positions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Another_team_positions_are_invisible_and_cannot_be_changed()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var otherPositionId = await Host.CreateJobPositionAsync(otherTeam.Id);
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        var positions = await manager.PositionsAsync();
        using var viewed = await manager.GetAsync(JobPositionApi.Position(otherPositionId));
        using var edited = await manager.EditAsync(otherPositionId, JobPositionApi.ValidInput(), version: 0);
        using var deleted = await manager.DeleteAsync(JobPositionApi.Position(otherPositionId));

        // Assert
        positions.ShouldBeEmpty();
        await viewed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await edited.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await deleted.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        var otherManagerUser = await Host.CreateUserAsync(UserBuilder.Manager(otherTeam.Id));
        using var otherManager = await CreateBrowserAsync(new SignedInStaff(otherManagerUser, await Host.SignInAsync(otherManagerUser)));
        (await otherManager.PositionsAsync()).ShouldHaveSingleItem().Id.ShouldBe(otherPositionId.Value);
    }

    [Fact]
    public async Task Positions_are_listed_by_state_open_by_default_and_newest_first()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var teamId = managerStaff.User.TeamId!.Value;
        var older = await Host.CreateJobPositionAsync(teamId, new JobPositionBuilder().WithTitle("Fake Older"));
        Host.Time.Advance(TimeSpan.FromMinutes(1));
        var newer = await Host.CreateJobPositionAsync(teamId, new JobPositionBuilder().WithTitle("Fake Newer"));
        var closed = await Host.CreateJobPositionAsync(teamId, new JobPositionBuilder().WithTitle("Fake Closed").Closed());
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        var byDefault = await manager.PositionsAsync();
        var open = await manager.PositionsAsync(JobPositionApi.PositionsIn(JobPositionState.Open));
        var closedOnes = await manager.PositionsAsync(JobPositionApi.PositionsIn(JobPositionState.Closed));

        // Assert
        byDefault.Select(position => position.Id).ShouldBe([newer.Value, older.Value]);
        open.ShouldBe(byDefault);
        closedOnes.ShouldHaveSingleItem().ShouldBe(new JobPositionSummaryView(closed.Value, "Fake Closed", JobPositionState.Closed, Host.Time.GetUtcNow()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_title_is_required(string? title)
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.CreateAsync(JobPositionApi.ValidInput() with { Title = title });

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        (await manager.PositionsAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \n ")]
    public async Task Requirements_are_required(string? requirements)
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.CreateAsync(JobPositionApi.ValidInput() with { Requirements = requirements });

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        (await manager.PositionsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Overlong_titles_and_sections_are_refused()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var input = JobPositionApi.ValidInput();

        // Act
        using var longTitle = await manager.CreateAsync(input with { Title = new string('x', JobPositionDetails.TitleMaxLength + 1) });
        using var longSection = await manager.CreateAsync(input with { Description = new string('x', JobPositionDetails.SectionMaxLength + 1) });

        // Assert
        await longTitle.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await longSection.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task Manager_edits_an_open_position()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);
        var before = await manager.PositionAsync(positionId);
        var input = new JobPositionInput("Fake Senior Developer", null, "- Five fake years", "- Fake cloud", "Ask about fake scaling.");

        // Act
        using var response = await manager.EditAsync(positionId, input, before.Version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var after = await manager.PositionAsync(positionId);
        after.ShouldBe(before with
        {
            Title = "Fake Senior Developer",
            Description = null,
            Requirements = "- Five fake years",
            NiceToHaves = "- Fake cloud",
            InterviewerNotes = "Ask about fake scaling.",
            Version = after.Version,
        });
        after.Version.ShouldNotBe(before.Version);
    }

    [Fact]
    public async Task A_stale_edit_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);
        var staleVersion = (await manager.PositionAsync(positionId)).Version;
        using var first = await manager.EditAsync(positionId, JobPositionApi.ValidInput("Fake First Edit"), staleVersion);
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        using var response = await manager.EditAsync(positionId, JobPositionApi.ValidInput("Fake Second Edit"), staleVersion);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.ChangedMeanwhile");
        (await manager.PositionAsync(positionId)).Title.ShouldBe("Fake First Edit");
    }

    [Fact]
    public async Task A_closed_position_cannot_be_edited()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value, new JobPositionBuilder().WithTitle("Fake Closed").Closed());
        using var manager = await CreateBrowserAsync(managerStaff);
        var version = (await manager.PositionAsync(positionId)).Version;

        // Act
        using var response = await manager.EditAsync(positionId, JobPositionApi.ValidInput("Fake Reopened"), version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        (await manager.PositionAsync(positionId)).Title.ShouldBe("Fake Closed");
    }

    [Fact]
    public async Task Manager_deletes_an_open_position()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        using var response = await manager.DeleteAsync(JobPositionApi.Position(positionId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var viewed = await manager.GetAsync(JobPositionApi.Position(positionId));
        await viewed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
    }

    [Fact]
    public async Task A_closed_position_cannot_be_deleted()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value, new JobPositionBuilder().Closed());
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        using var response = await manager.DeleteAsync(JobPositionApi.Position(positionId));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        (await manager.PositionsAsync(JobPositionApi.PositionsIn(JobPositionState.Closed))).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_position_with_candidates_cannot_be_deleted()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        await Host.AddCandidateAsync(positionId);
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        using var response = await manager.DeleteAsync(JobPositionApi.Position(positionId));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.HasCandidates");
        (await manager.PositionAsync(positionId)).State.ShouldBe(JobPositionState.Open);
    }

    [Fact]
    public async Task An_unknown_position_is_not_found()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var unknown = JobPositionId.New();

        // Act
        using var viewed = await manager.GetAsync(JobPositionApi.Position(unknown));
        using var edited = await manager.EditAsync(unknown, JobPositionApi.ValidInput(), version: 1);
        using var deleted = await manager.DeleteAsync(JobPositionApi.Position(unknown));

        // Assert
        await viewed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await edited.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await deleted.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
    }
}
