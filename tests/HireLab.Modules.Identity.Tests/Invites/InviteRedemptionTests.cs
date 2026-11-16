using System.Net;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Invites.ListInvites;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Invites;

public sealed class InviteRedemptionTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string StaffCookie = "__Host-hirelab-staff";

    [Fact]
    public async Task Redemption_logs_the_new_user_in_with_a_working_antiforgery_token()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync();
        using var invitee = await CreateBrowserAsync();
        var anonymousToken = invitee.Cookie(AntiforgeryTokens.CookieName);

        // Act
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        invitee.Cookie(StaffCookie).ShouldNotBeNull();
        invitee.Cookie(AntiforgeryTokens.CookieName).ShouldNotBe(anonymousToken);
        using var me = await invitee.GetAsync(AuthApi.Me);
        (await me.CurrentUserAsync()).Email.ShouldBe(issued.Invite.Email);
        using var logout = await invitee.PostAsync(AuthApi.Logout);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var login = await invitee.LoginAsync(issued.Invite.Email, InviteApi.ValidPassword);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_link_works_only_once()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync();
        using var invitee = await CreateBrowserAsync();
        using var first = await invitee.RedeemAsync(issued.Token);
        using var someoneElse = await CreateBrowserAsync();

        // Act
        using var preview = await someoneElse.PreviewAsync(issued.Token);
        using var second = await someoneElse.RedeemAsync(issued.Token, "another-password-not-a-secret");

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        await preview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.AlreadyUsed");
        await second.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.AlreadyUsed");
        someoneElse.Cookie(StaffCookie).ShouldBeNull();
    }

    [Fact]
    public async Task A_link_expires_after_72_hours()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync();
        using var invitee = await CreateBrowserAsync();
        Host.Time.Advance(TimeSpan.FromHours(72) - TimeSpan.FromSeconds(1));
        using var stillPending = await invitee.PreviewAsync(issued.Token);
        Host.Time.Advance(TimeSpan.FromSeconds(1));

        // Act
        using var preview = await invitee.PreviewAsync(issued.Token);
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        stillPending.StatusCode.ShouldBe(HttpStatusCode.OK);
        await preview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Expired");
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Expired");
        (await InviteStateAsync(issued.Invite.Id)).ShouldBe(OneTimeLinkState.Expired);
    }

    [Fact]
    public async Task An_unknown_link_is_not_found()
    {
        // Arrange
        using var invitee = await CreateBrowserAsync();

        // Act
        using var preview = await invitee.PreviewAsync("not-a-real-invite-token");
        using var redeemed = await invitee.RedeemAsync("not-a-real-invite-token");

        // Assert
        await preview.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invite.LinkNotFound");
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invite.LinkNotFound");
    }

    [Fact]
    public async Task Redemption_is_refused_when_the_email_was_taken_after_the_invite_was_issued()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync("late@example.com");
        var team = await Host.CreateTeamAsync();
        await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).WithEmail("late@example.com"));
        using var invitee = await CreateBrowserAsync();

        // Act
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.EmailInUse");
        (await InviteStateAsync(issued.Invite.Id)).ShouldBe(OneTimeLinkState.Pending);
    }

    [Fact]
    public async Task A_rejected_password_leaves_the_invite_pending_and_creates_nothing()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var issued = await (await admin.InviteManagerToNewTeamAsync()).IssuedAsync();
        using var invitee = await CreateBrowserAsync();

        // Act
        using var tooShort = await invitee.RedeemAsync(issued.Token, "short");
        using var retried = await invitee.RedeemAsync(issued.Token);

        // Assert
        await tooShort.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Invite.PasswordRejected");
        retried.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var scope = Host.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().Teams.CountAsync(CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task An_empty_password_is_a_validation_error()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync();
        using var invitee = await CreateBrowserAsync();

        // Act
        using var redeemed = await invitee.RedeemAsync(issued.Token, string.Empty);

        // Assert
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task Only_a_hash_of_the_token_is_stored()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync();

        // Act
        await using var scope = Host.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().Invites
            .SingleAsync(invite => invite.Id == new InviteId(issued.Invite.Id), CancellationToken);

        // Assert
        stored.TokenHash.ShouldBe(OneTimeToken.HashOf(issued.Token));
        stored.TokenHash.ShouldNotContain(issued.Token);
    }

    [Fact]
    public async Task Redemption_without_the_antiforgery_header_is_refused()
    {
        // Arrange
        var issued = await IssueInterviewerInviteAsync();
        using var invitee = await CreateBrowserAsync();
        invitee.SendsAntiforgeryHeader = false;

        // Act
        using var redeemed = await invitee.RedeemAsync(issued.Token);

        // Assert
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Antiforgery.InvalidToken");
        (await InviteStateAsync(issued.Invite.Id)).ShouldBe(OneTimeLinkState.Pending);
    }

    [Fact]
    public async Task More_than_ten_redemption_attempts_a_minute_from_one_address_are_rate_limited()
    {
        // Arrange
        using var invitee = await CreateBrowserAsync();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var allowed = await invitee.PreviewAsync("not-a-real-invite-token");
            allowed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // Act
        using var limited = await invitee.RedeemAsync("not-a-real-invite-token");

        // Assert
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task The_admin_bootstrap_invite_creates_the_admin_without_a_team()
    {
        // Arrange
        var token = await IssueAdminInviteAsync("first.admin@example.com");
        using var invitee = await CreateBrowserAsync();

        // Act
        using var redeemed = await invitee.RedeemAsync(token);

        // Assert
        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await redeemed.CurrentUserAsync();
        user.Role.ShouldBe(StaffRole.Admin);
        user.Team.ShouldBeNull();
    }

    [Fact]
    public async Task The_admin_bootstrap_invite_is_refused_when_an_admin_exists()
    {
        // Arrange
        var token = await IssueAdminInviteAsync("second.admin@example.com");
        await Host.CreateUserAsync(UserBuilder.Admin());
        using var invitee = await CreateBrowserAsync();

        // Act
        using var redeemed = await invitee.RedeemAsync(token);

        // Assert
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.AdminExists");
    }

    [Fact]
    public async Task Invites_drop_off_the_list_30_days_after_they_expire()
    {
        // Arrange
        var admin = await SignInAsAsync(StaffRole.Admin);
        using (var browser = await CreateBrowserAsync(admin))
        {
            await (await browser.InviteManagerToNewTeamAsync()).IssuedAsync();
        }

        Host.Time.Advance(TimeSpan.FromHours(72) + ListInvitesHandler.ListedAfterExpiry - TimeSpan.FromSeconds(1));
        var stillListed = await ListAsAsync(admin.User);
        Host.Time.Advance(TimeSpan.FromSeconds(1));

        // Act
        var invites = await ListAsAsync(admin.User);

        // Assert
        stillListed.Single().State.ShouldBe("Expired");
        invites.ShouldBeEmpty();
    }

    private async Task<IssuedInviteView> IssueInterviewerInviteAsync(string email = "new.interviewer@example.com")
    {
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        return await (await manager.InviteInterviewerAsync(email)).IssuedAsync();
    }

    private async Task<IReadOnlyList<InviteView>> ListAsAsync(TestUser admin)
    {
        using var browser = await CreateBrowserAsync(new SignedInStaff(admin, await Host.SignInAsync(admin)));
        return await browser.ListAsync(InviteApi.ManagerInvites);
    }

    private async Task<string> IssueAdminInviteAsync(string email)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var normalizer = scope.ServiceProvider.GetRequiredService<ILookupNormalizer>();
        var token = OneTimeToken.New();
        dbContext.Invites.Add(Invite.ForAdmin(
            new Invitee(email, normalizer.NormalizeEmail(email), "Fake First Admin"), token, Host.Time.GetUtcNow()));
        await dbContext.SaveChangesAsync(CancellationToken);
        return token.Value;
    }

    private async Task<OneTimeLinkState> InviteStateAsync(Guid inviteId)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var invite = await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().Invites
            .SingleAsync(invite => invite.Id == new InviteId(inviteId), CancellationToken);
        return invite.StateAt(Host.Time.GetUtcNow());
    }
}
