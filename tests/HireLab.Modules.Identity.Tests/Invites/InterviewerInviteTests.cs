using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;

namespace HireLab.Modules.Identity.Tests.Invites;

public sealed class InterviewerInviteTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Manager_invites_an_interviewer_into_their_team_and_the_link_redeems_into_that_team()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Epsilon"));
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var managerBrowser = await CreateBrowserAsync(new SignedInStaff(manager, await Host.SignInAsync(manager)));

        // Act
        var issued = await (await managerBrowser.InviteInterviewerAsync("ivan@example.com", "Ivan Interviewer")).IssuedAsync();
        using var invitee = await CreateBrowserAsync();
        using var preview = await invitee.PreviewAsync(issued.Token);
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        issued.Invite.Role.ShouldBe(StaffRole.Interviewer);
        issued.Invite.Team.ShouldBe(new InviteTeam(team.Id.Value, "Fake Team Epsilon", IsNew: false));
        (await preview.Content.ReadFromJsonAsync<InvitePreview>(CancellationToken)).ShouldBe(new InvitePreview(
            "ivan@example.com", "Ivan Interviewer", StaffRole.Interviewer, "Fake Team Epsilon", IsNewTeam: false, issued.Invite.ExpiresAt));
        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await redeemed.CurrentUserAsync();
        user.Role.ShouldBe(StaffRole.Interviewer);
        user.Team.ShouldBe(new CurrentUserTeam(team.Id.Value, "Fake Team Epsilon"));
        (await managerBrowser.ListAsync(InviteApi.InterviewerInvites)).Single().State.ShouldBe("Used");
    }

    [Theory]
    [InlineData(StaffRole.Admin)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Only_a_manager_invites_interviewers(StaffRole role)
    {
        // Arrange
        using var browser = await CreateBrowserAsync(await SignInAsAsync(role));

        // Act
        using var response = await browser.InviteInterviewerAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Another_teams_manager_neither_sees_nor_revokes_the_invite()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var issued = await (await manager.InviteInterviewerAsync()).IssuedAsync();
        using var otherManager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        var listed = await otherManager.ListAsync(InviteApi.InterviewerInvites);
        using var revoke = await otherManager.PostAsync(InviteApi.RevokeInterviewerInvite(issued.Invite.Id));

        // Assert
        listed.ShouldBeEmpty();
        await revoke.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invite.NotFound");
        (await manager.ListAsync(InviteApi.InterviewerInvites)).Single().State.ShouldBe("Pending");
    }

    [Fact]
    public async Task Manager_cannot_revoke_a_manager_invite_through_the_interviewer_endpoint()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var issued = await (await admin.InviteManagerToNewTeamAsync()).IssuedAsync();
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.PostAsync(InviteApi.RevokeInterviewerInvite(issued.Invite.Id));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invite.NotFound");
    }

    [Fact]
    public async Task Manager_revokes_their_teams_invite()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var issued = await (await manager.InviteInterviewerAsync()).IssuedAsync();

        // Act
        using var response = await manager.PostAsync(InviteApi.RevokeInterviewerInvite(issued.Invite.Id));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(issued.Token);
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Revoked");
    }

    [Fact]
    public async Task Interviewer_invite_with_an_invalid_email_is_a_validation_error()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.InviteInterviewerAsync(email: "not-an-email", displayName: string.Empty);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }
}
