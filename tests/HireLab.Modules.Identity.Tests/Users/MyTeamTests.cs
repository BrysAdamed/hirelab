using System.Net;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;

namespace HireLab.Modules.Identity.Tests.Users;

public sealed class MyTeamTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Manager_lists_only_their_teams_interviewers()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Zeta"));
        var managerStaff = await SignInAsManagerOfAsync(team.Id);
        var active = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).WithDisplayName("Ada Active"));
        var inactive = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).WithDisplayName("Ike Inactive").Inactive());
        await Host.CreateUserAsync(UserBuilder.Interviewer((await Host.CreateTeamAsync()).Id));
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        var interviewers = await manager.UsersAsync(UserApi.MyTeamInterviewers);

        // Assert
        var teamView = new UserTeamView(team.Id.Value, "Fake Team Zeta");
        interviewers.ShouldBe(
        [
            new UserView(active.Id.Value, active.Email, "Ada Active", StaffRole.Interviewer, teamView, IsActive: true),
            new UserView(inactive.Id.Value, inactive.Email, "Ike Inactive", StaffRole.Interviewer, teamView, IsActive: false),
        ]);
    }

    [Fact]
    public async Task Manager_deactivates_their_interviewer_who_then_cannot_log_in_until_reactivated()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        using var manager = await CreateBrowserAsync(await SignInAsManagerOfAsync(team.Id));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        var stampBefore = (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp;
        using var browser = await CreateBrowserAsync();

        // Act
        using var deactivated = await manager.PostAsync(UserApi.DeactivateInterviewer(interviewer.Id));
        var stampAfterDeactivation = (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp;
        using var refusedLogin = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        using var reactivated = await manager.PostAsync(UserApi.ReactivateInterviewer(interviewer.Id));
        using var login = await browser.LoginAsync(interviewer.Email, interviewer.Password);

        // Assert
        deactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        stampAfterDeactivation.ShouldNotBe(stampBefore);
        await refusedLogin.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Auth.InvalidCredentials");
        reactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp.ShouldNotBe(stampAfterDeactivation);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_cannot_touch_another_teams_interviewer()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var otherTeam = await Host.CreateTeamAsync();
        var active = await Host.CreateUserAsync(UserBuilder.Interviewer(otherTeam.Id));
        var inactive = await Host.CreateUserAsync(UserBuilder.Interviewer(otherTeam.Id).Inactive());

        // Act
        using var deactivate = await manager.PostAsync(UserApi.DeactivateInterviewer(active.Id));
        using var reactivate = await manager.PostAsync(UserApi.ReactivateInterviewer(inactive.Id));

        // Assert
        await deactivate.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
        await reactivate.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
        (await Host.StoredUserAsync(active.Id)).IsActive.ShouldBeTrue();
        (await Host.StoredUserAsync(inactive.Id)).IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Manager_can_deactivate_neither_themselves_nor_another_manager_of_their_team()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var managerStaff = await SignInAsManagerOfAsync(team.Id);
        var formerManager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id).Inactive());
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        using var self = await manager.PostAsync(UserApi.DeactivateInterviewer(managerStaff.User.Id));
        using var former = await manager.PostAsync(UserApi.ReactivateInterviewer(formerManager.Id));

        // Assert
        await self.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
        await former.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
        (await Host.StoredUserAsync(managerStaff.User.Id)).IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(StaffRole.Admin)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Only_a_manager_manages_their_team(StaffRole role)
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var browser = await CreateBrowserAsync(await SignInAsAsync(role));

        // Act
        using var list = await browser.GetAsync(UserApi.MyTeamInterviewers);
        using var deactivate = await browser.PostAsync(UserApi.DeactivateInterviewer(interviewer.Id));
        using var reactivate = await browser.PostAsync(UserApi.ReactivateInterviewer(interviewer.Id));

        // Assert
        new[] { list, deactivate, reactivate }.ShouldAllBe(response => response.StatusCode == HttpStatusCode.Forbidden);
        (await Host.StoredUserAsync(interviewer.Id)).IsActive.ShouldBeTrue();
    }

    private async Task<SignedInStaff> SignInAsManagerOfAsync(TeamId teamId)
    {
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(teamId));
        return new SignedInStaff(manager, await Host.SignInAsync(manager));
    }
}
