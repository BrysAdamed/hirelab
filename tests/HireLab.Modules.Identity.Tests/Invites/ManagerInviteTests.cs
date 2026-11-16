using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Invites;

public sealed class ManagerInviteTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Admin_invites_a_manager_into_a_new_team_which_is_created_only_on_redemption()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        var issued = await (await admin.InviteManagerToNewTeamAsync("nora@example.com", "Nora Newmanager", "Fake Team Beta")).IssuedAsync();
        var teamsBeforeRedemption = await TeamCountAsync();
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        issued.LinkPath.ShouldStartWith("/invite#");
        issued.Invite.ShouldBe(issued.Invite with
        {
            Email = "nora@example.com",
            DisplayName = "Nora Newmanager",
            Role = StaffRole.Manager,
            Team = new InviteTeam(null, "Fake Team Beta", IsNew: true),
            ExpiresAt = Host.Time.GetUtcNow().AddHours(72),
            State = "Pending",
        });
        teamsBeforeRedemption.ShouldBe(0);

        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await redeemed.CurrentUserAsync();
        user.Email.ShouldBe("nora@example.com");
        user.DisplayName.ShouldBe("Nora Newmanager");
        user.Role.ShouldBe(StaffRole.Manager);
        user.Team.ShouldNotBeNull().Name.ShouldBe("Fake Team Beta");
        (await TeamCountAsync()).ShouldBe(1);
        (await admin.ListAsync(InviteApi.ManagerInvites)).Single().State.ShouldBe("Used");
    }

    [Fact]
    public async Task Admin_hands_a_team_without_an_active_manager_over_to_a_new_manager()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Gamma"));
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id).Inactive());
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        var issued = await (await admin.InviteManagerToTeamAsync(team.Id)).IssuedAsync();
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        issued.Invite.Team.ShouldBe(new InviteTeam(team.Id.Value, "Fake Team Gamma", IsNew: false));
        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await redeemed.CurrentUserAsync()).Team.ShouldBe(new CurrentUserTeam(team.Id.Value, "Fake Team Gamma"));
        (await TeamCountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Manager_invite_into_a_team_with_an_active_manager_is_refused()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.InviteManagerToTeamAsync(team.Id);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.TeamHasManager");
    }

    [Fact]
    public async Task Manager_invite_is_refused_at_redemption_when_the_team_got_an_active_manager_meanwhile()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var issued = await (await admin.InviteManagerToTeamAsync(team.Id)).IssuedAsync();
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id));
        using var invitee = await CreateBrowserAsync();

        // Act
        using var response = await invitee.RedeemAsync(issued.Token);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.TeamHasManager");
        (await admin.ListAsync(InviteApi.ManagerInvites)).Single().State.ShouldBe("Pending");
    }

    [Fact]
    public async Task Manager_invite_into_an_unknown_team_is_not_found()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.InviteManagerToTeamAsync(TeamId.New());

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invite.TeamNotFound");
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, "Fake Team Delta")]
    public async Task Manager_invite_needs_exactly_one_of_a_new_team_or_an_existing_team(bool withTeamId, string? newTeamName)
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.PostAsJsonAsync(InviteApi.ManagerInvites, new
        {
            email = "someone@example.com",
            displayName = "Some One",
            teamId = withTeamId ? team.Id.Value : (Guid?)null,
            newTeamName,
        });

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task Invite_for_the_email_of_an_existing_user_is_refused()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var existing = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).WithEmail("taken@example.com"));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.InviteManagerToNewTeamAsync(email: "Taken@example.com");

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.EmailInUse");
        existing.Email.ShouldBe("taken@example.com");
    }

    [Fact]
    public async Task Issuing_a_new_invite_for_the_same_email_revokes_the_old_one()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var first = await (await admin.InviteManagerToNewTeamAsync("twice@example.com", teamName: "Fake Team One")).IssuedAsync();

        // Act
        var second = await (await admin.InviteManagerToNewTeamAsync("TWICE@example.com", teamName: "Fake Team Two")).IssuedAsync();

        // Assert
        var invites = await admin.ListAsync(InviteApi.ManagerInvites);
        invites.Single(invite => invite.Id == first.Invite.Id).State.ShouldBe("Revoked");
        invites.Single(invite => invite.Id == second.Invite.Id).State.ShouldBe("Pending");

        using var invitee = await CreateBrowserAsync();
        using var oldLink = await invitee.RedeemAsync(first.Token);
        await oldLink.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Revoked");
        using var newLink = await invitee.RedeemAsync(second.Token);
        (await newLink.CurrentUserAsync()).Team.ShouldNotBeNull().Name.ShouldBe("Fake Team Two");
    }

    [Fact]
    public async Task Admin_revokes_a_pending_invite_and_its_link_stops_working()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var issued = await (await admin.InviteManagerToNewTeamAsync()).IssuedAsync();

        // Act
        using var revoked = await admin.PostAsync(InviteApi.RevokeManagerInvite(issued.Invite.Id));

        // Assert
        revoked.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.ListAsync(InviteApi.ManagerInvites)).Single().State.ShouldBe("Revoked");
        using var invitee = await CreateBrowserAsync();
        using var preview = await invitee.PreviewAsync(issued.Token);
        await preview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Revoked");
        (await TeamCountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_used_invite_cannot_be_revoked()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var issued = await (await admin.InviteManagerToNewTeamAsync()).IssuedAsync();
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Act
        using var response = await admin.PostAsync(InviteApi.RevokeManagerInvite(issued.Invite.Id));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.NotOpen");
    }

    [Fact]
    public async Task Revoking_an_unknown_invite_is_not_found()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.PostAsync(InviteApi.RevokeManagerInvite(Guid.CreateVersion7()));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invite.NotFound");
    }

    [Fact]
    public async Task Teams_without_an_active_manager_are_listed_for_handover()
    {
        // Arrange
        var managed = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Managed"));
        await Host.CreateUserAsync(UserBuilder.Manager(managed.Id));
        var handedOver = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Orphaned"));
        await Host.CreateUserAsync(UserBuilder.Manager(handedOver.Id).Inactive());
        var empty = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Empty"));
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.GetAsync(InviteApi.TeamsWithoutManager);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<TeamWithoutManager>>(CancellationToken)).ShouldBe(
        [
            new TeamWithoutManager(empty.Id.Value, "Fake Team Empty"),
            new TeamWithoutManager(handedOver.Id.Value, "Fake Team Orphaned"),
        ]);
    }

    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Only_the_admin_invites_managers(StaffRole role)
    {
        // Arrange
        using var browser = await CreateBrowserAsync(await SignInAsAsync(role));

        // Act
        using var response = await browser.InviteManagerToNewTeamAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TeamCountAsync()).ShouldBe(1);
    }

    private async Task<int> TeamCountAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().Teams.CountAsync(CancellationToken);
    }
}
