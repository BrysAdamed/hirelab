using System.Net;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.Modules.Identity.Tests.Invites;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;

namespace HireLab.Modules.Identity.Tests.Users;

public sealed class UserManagementTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Admin_lists_every_user_with_their_team_and_state()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Alpha"));
        var adminStaff = await SignInAsAsync(StaffRole.Admin);
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id).WithDisplayName("Bea Manager"));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).WithDisplayName("Cid Interviewer").Inactive());
        using var admin = await CreateBrowserAsync(adminStaff);

        // Act
        var users = await admin.UsersAsync(UserApi.Users);

        // Assert
        var teamView = new UserTeamView(team.Id.Value, "Fake Team Alpha");
        users.ShouldBe(
        [
            new UserView(manager.Id.Value, manager.Email, "Bea Manager", StaffRole.Manager, teamView, IsActive: true),
            new UserView(interviewer.Id.Value, interviewer.Email, "Cid Interviewer", StaffRole.Interviewer, teamView, IsActive: false),
            new UserView(adminStaff.User.Id.Value, adminStaff.User.Email, adminStaff.User.DisplayName, StaffRole.Admin, null, IsActive: true),
        ]);
    }

    [Fact]
    public async Task Admin_deactivates_a_user_who_then_cannot_log_in()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        var stampBefore = (await Host.StoredUserAsync(manager.Id)).SecurityStamp;
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.PostAsync(UserApi.Deactivate(manager.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stored = await Host.StoredUserAsync(manager.Id);
        stored.IsActive.ShouldBeFalse();
        stored.SecurityStamp.ShouldNotBe(stampBefore);
        using var browser = await CreateBrowserAsync();
        using var login = await browser.LoginAsync(manager.Email, manager.Password);
        await login.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Admin_cannot_deactivate_themselves()
    {
        // Arrange
        var adminStaff = await SignInAsAsync(StaffRole.Admin);
        using var admin = await CreateBrowserAsync(adminStaff);

        // Act
        using var response = await admin.PostAsync(UserApi.Deactivate(adminStaff.User.Id));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "User.CannotDeactivateSelf");
        (await Host.StoredUserAsync(adminStaff.User.Id)).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Admin_reactivates_a_user_who_can_log_in_again()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).Inactive());
        var stampBefore = (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp;
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.PostAsync(UserApi.Reactivate(interviewer.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stored = await Host.StoredUserAsync(interviewer.Id);
        stored.IsActive.ShouldBeTrue();
        stored.SecurityStamp.ShouldNotBe(stampBefore);
        using var browser = await CreateBrowserAsync();
        using var login = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivating_or_reactivating_twice_changes_nothing_the_second_time()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        (await admin.PostAsync(UserApi.Deactivate(interviewer.Id))).Dispose();
        var stampAfterFirst = (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp;

        // Act
        using var deactivatedAgain = await admin.PostAsync(UserApi.Deactivate(interviewer.Id));
        var stampAfterSecond = (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp;
        using var reactivated = await admin.PostAsync(UserApi.Reactivate(interviewer.Id));
        using var reactivatedAgain = await admin.PostAsync(UserApi.Reactivate(interviewer.Id));

        // Assert
        deactivatedAgain.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        stampAfterSecond.ShouldBe(stampAfterFirst);
        reactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        reactivatedAgain.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Host.StoredUserAsync(interviewer.Id)).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Reactivating_a_manager_is_refused_while_their_team_has_another_active_manager()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var oldManager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id).Inactive());
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.PostAsync(UserApi.Reactivate(oldManager.Id));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "User.TeamHasManager");
        (await Host.StoredUserAsync(oldManager.Id)).IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handover_deactivates_the_old_manager_and_a_new_manager_is_invited_into_the_same_team()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Gamma"));
        var oldManager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        (await admin.PostAsync(UserApi.Deactivate(oldManager.Id))).Dispose();
        var teamWithoutManager = (await admin.TeamsAsync()).Single(view => view.Id == team.Id.Value);
        var issued = await (await admin.InviteManagerToTeamAsync(team.Id)).IssuedAsync();
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(issued.Token);
        using var reactivateOld = await admin.PostAsync(UserApi.Reactivate(oldManager.Id));

        // Assert
        teamWithoutManager.Manager.ShouldBeNull();
        teamWithoutManager.Members.Single().IsActive.ShouldBeFalse();
        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var teamAfter = (await admin.TeamsAsync()).Single(view => view.Id == team.Id.Value);
        teamAfter.Manager.ShouldNotBeNull().Email.ShouldBe("handover.manager@example.com");
        await reactivateOld.ShouldBeProblemAsync(HttpStatusCode.Conflict, "User.TeamHasManager");
    }

    [Fact]
    public async Task Deactivating_a_manager_revokes_the_pending_invites_they_issued()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var managerBrowser = await CreateBrowserAsync(new SignedInStaff(manager, await Host.SignInAsync(manager)));
        var used = await (await managerBrowser.InviteInterviewerAsync("used@example.com", "Uma Used")).IssuedAsync();
        using var usedBy = await CreateBrowserAsync();
        (await usedBy.RedeemAsync(used.Token)).Dispose();
        var pending = await (await managerBrowser.InviteInterviewerAsync("pending@example.com", "Pia Pending")).IssuedAsync();
        using var otherManager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var othersInvite = await (await otherManager.InviteInterviewerAsync("other@example.com", "Oto Other")).IssuedAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.PostAsync(UserApi.Deactivate(manager.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(pending.Token);
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Revoked");
        (await otherManager.ListAsync(InviteApi.InterviewerInvites)).Single(invite => invite.Id == othersInvite.Invite.Id)
            .State.ShouldBe("Pending");
        using var usedPreview = await invitee.PreviewAsync(used.Token);
        await usedPreview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.AlreadyUsed");
    }

    [Fact]
    public async Task Admin_reassigns_an_interviewer_to_another_team_without_touching_invites()
    {
        // Arrange
        var oldTeam = await Host.CreateTeamAsync();
        var newTeam = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Delta"));
        var oldManager = await Host.CreateUserAsync(UserBuilder.Manager(oldTeam.Id));
        using var oldManagerBrowser = await CreateBrowserAsync(new SignedInStaff(oldManager, await Host.SignInAsync(oldManager)));
        var pending = await (await oldManagerBrowser.InviteInterviewerAsync()).IssuedAsync();
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(oldTeam.Id));
        var stampBefore = (await Host.StoredUserAsync(interviewer.Id)).SecurityStamp;
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.ReassignAsync(interviewer.Id, newTeam.Id);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stored = await Host.StoredUserAsync(interviewer.Id);
        stored.TeamId.ShouldBe(newTeam.Id);
        stored.SecurityStamp.ShouldNotBe(stampBefore);
        (await oldManagerBrowser.ListAsync(InviteApi.InterviewerInvites)).Single(invite => invite.Id == pending.Invite.Id)
            .State.ShouldBe("Pending");
        using var browser = await CreateBrowserAsync();
        using var login = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        (await login.CurrentUserAsync()).Team.ShouldBe(new CurrentUserTeam(newTeam.Id.Value, "Fake Team Delta"));
    }

    [Fact]
    public async Task Only_an_interviewer_can_be_reassigned()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var otherTeam = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.ReassignAsync(manager.Id, otherTeam.Id);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "User.NotAnInterviewer");
        (await Host.StoredUserAsync(manager.Id)).TeamId.ShouldBe(team.Id);
    }

    [Fact]
    public async Task Reassigning_to_an_unknown_team_is_not_found()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.ReassignAsync(interviewer.Id, TeamId.New());

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Team.NotFound");
    }

    [Fact]
    public async Task Actions_on_an_unknown_user_are_not_found()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var unknown = UserId.New();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var deactivate = await admin.PostAsync(UserApi.Deactivate(unknown));
        using var reactivate = await admin.PostAsync(UserApi.Reactivate(unknown));
        using var reassign = await admin.ReassignAsync(unknown, team.Id);

        // Assert
        await deactivate.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
        await reactivate.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
        await reassign.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
    }

    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Only_the_admin_manages_users_and_teams(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var team = staff.User.TeamId!.Value;
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team));
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var list = await browser.GetAsync(UserApi.Users);
        using var deactivate = await browser.PostAsync(UserApi.Deactivate(interviewer.Id));
        using var reactivate = await browser.PostAsync(UserApi.Reactivate(interviewer.Id));
        using var reassign = await browser.ReassignAsync(interviewer.Id, team);
        using var teams = await browser.GetAsync(UserApi.Teams);
        using var rename = await browser.RenameTeamAsync(team, "Fake Team Renamed");

        // Assert
        new[] { list, deactivate, reactivate, reassign, teams, rename }
            .ShouldAllBe(response => response.StatusCode == HttpStatusCode.Forbidden);
        (await Host.StoredUserAsync(interviewer.Id)).IsActive.ShouldBeTrue();
    }
}
