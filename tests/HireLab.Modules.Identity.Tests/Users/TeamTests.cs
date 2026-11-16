using System.Net;
using HireLab.Modules.Identity.Tests.Invites;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;

namespace HireLab.Modules.Identity.Tests.Users;

public sealed class TeamTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Admin_lists_teams_with_their_manager_and_members()
    {
        // Arrange
        var staffed = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Alpha"));
        var orphaned = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Beta"));
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(staffed.Id).WithDisplayName("Zed Manager"));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(staffed.Id).WithDisplayName("Amy Interviewer"));
        var formerManager = await Host.CreateUserAsync(UserBuilder.Manager(orphaned.Id).Inactive());
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        var teams = await admin.TeamsAsync();

        // Assert
        teams.Select(team => team.Name).ShouldBe(["Fake Team Alpha", "Fake Team Beta"]);
        teams[0].Manager.ShouldNotBeNull().Id.ShouldBe(manager.Id.Value);
        teams[0].Members.Select(member => member.Id).ShouldBe([manager.Id.Value, interviewer.Id.Value]);
        teams[1].Manager.ShouldBeNull();
        teams[1].Members.ShouldHaveSingleItem().ShouldBe(new UserView(
            formerManager.Id.Value,
            formerManager.Email,
            formerManager.DisplayName,
            StaffRole.Manager,
            new UserTeamView(orphaned.Id.Value, "Fake Team Beta"),
            IsActive: false));
    }

    [Fact]
    public async Task Admin_renames_a_team()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Old"));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.RenameTeamAsync(team.Id, "  Fake Team New  ");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var renamed = (await admin.TeamsAsync()).ShouldHaveSingleItem();
        renamed.Id.ShouldBe(team.Id.Value);
        renamed.Name.ShouldBe("Fake Team New");
        renamed.Members.ShouldHaveSingleItem().Id.ShouldBe(interviewer.Id.Value);
    }

    [Fact]
    public async Task Renaming_an_unknown_team_is_not_found()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.RenameTeamAsync(TeamId.New(), "Fake Team Nowhere");

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Team.NotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_team_name_is_required(string name)
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Kept"));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.RenameTeamAsync(team.Id, name);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        (await admin.TeamsAsync()).Single().Name.ShouldBe("Fake Team Kept");
    }

    [Fact]
    public async Task A_team_name_longer_than_100_characters_is_refused()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.RenameTeamAsync(team.Id, new string('x', 101));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }
}
