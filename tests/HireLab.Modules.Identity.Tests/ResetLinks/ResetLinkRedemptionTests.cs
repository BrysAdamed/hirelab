using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.Modules.Identity.Tests.Invites;
using HireLab.Modules.Identity.Tests.Users;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.ResetLinks;

public sealed class ResetLinkRedemptionTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string StaffCookie = "__Host-hirelab-staff";
    private static readonly TimeSpan _pastValidationInterval = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Preview_shows_whose_password_the_link_sets()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();

        // Act
        using var response = await browser.PreviewResetLinkAsync(issued.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ResetLinkPreview>(CancellationToken))
            .ShouldBe(new ResetLinkPreview(interviewer.Email, interviewer.DisplayName, issued.ExpiresAt));
    }

    [Fact]
    public async Task Redemption_sets_the_new_password_without_logging_in()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();

        // Act
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        redeemed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        browser.Cookie(StaffCookie).ShouldBeNull();
        using var oldPassword = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        oldPassword.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var newPassword = await browser.LoginAsync(interviewer.Email, ResetLinkApi.NewPassword);
        newPassword.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Redemption_clears_the_lockout()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await browser.LoginAsync(interviewer.Email, "wrong-password-not-a-secret");
        }

        using var lockedOut = await browser.LoginAsync(interviewer.Email, interviewer.Password);

        // Act
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        lockedOut.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        redeemed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var login = await browser.LoginAsync(interviewer.Email, ResetLinkApi.NewPassword);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Redemption_ends_the_users_existing_sessions()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        using var session = Host.CreateBrowser(await Host.SignInAsync(interviewer));
        using var anonymous = await CreateBrowserAsync();

        // Act
        using var redeemed = await anonymous.RedeemResetLinkAsync(issued.Token);
        Host.Time.Advance(_pastValidationInterval);

        // Assert
        redeemed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var me = await session.GetAsync(AuthApi.Me);
        me.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_link_works_only_once()
    {
        // Arrange
        var (_, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();
        using var first = await browser.RedeemResetLinkAsync(issued.Token);

        // Act
        using var preview = await browser.PreviewResetLinkAsync(issued.Token);
        using var second = await browser.RedeemResetLinkAsync(issued.Token, "another-password-not-a-secret");

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await preview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.AlreadyUsed");
        await second.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.AlreadyUsed");
    }

    [Fact]
    public async Task A_link_expires_after_24_hours()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();
        Host.Time.Advance(TimeSpan.FromHours(24) - TimeSpan.FromSeconds(1));
        using var stillPending = await browser.PreviewResetLinkAsync(issued.Token);
        Host.Time.Advance(TimeSpan.FromSeconds(1));

        // Act
        using var preview = await browser.PreviewResetLinkAsync(issued.Token);
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        stillPending.StatusCode.ShouldBe(HttpStatusCode.OK);
        await preview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.Expired");
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.Expired");
        using var login = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_unknown_link_is_not_found()
    {
        // Arrange
        using var browser = await CreateBrowserAsync();

        // Act
        using var preview = await browser.PreviewResetLinkAsync("not-a-real-reset-token");
        using var redeemed = await browser.RedeemResetLinkAsync("not-a-real-reset-token");

        // Assert
        await preview.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.LinkNotFound");
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.LinkNotFound");
    }

    [Fact]
    public async Task A_rejected_password_leaves_the_link_pending_and_the_password_unchanged()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();

        // Act
        using var tooShort = await browser.RedeemResetLinkAsync(issued.Token, "short");

        // Assert
        await tooShort.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "ResetLink.PasswordRejected");
        using var login = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var retried = await browser.RedeemResetLinkAsync(issued.Token);
        retried.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task A_user_deactivated_after_the_link_was_issued_cannot_redeem_it()
    {
        // Arrange
        var (interviewer, issued) = await IssueForInterviewerAsync();
        await DeactivateAsync(interviewer.Id);
        using var browser = await CreateBrowserAsync();

        // Act
        using var preview = await browser.PreviewResetLinkAsync(issued.Token);
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        await preview.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.UserInactive");
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.UserInactive");
    }

    [Fact]
    public async Task Deactivating_the_user_revokes_their_link_so_it_fails_after_reactivation()
    {
        // Arrange
        var (_, interviewer, issued) = await IssueAsManagerAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        using var deactivated = await admin.PostAsync(UserApi.Deactivate(interviewer.Id));
        using var reactivated = await admin.PostAsync(UserApi.Reactivate(interviewer.Id));
        using var browser = await CreateBrowserAsync();

        // Act
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        deactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        reactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.Revoked");
    }

    [Fact]
    public async Task Deactivating_the_issuer_revokes_the_links_they_issued()
    {
        // Arrange
        var (manager, _, issued) = await IssueAsManagerAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        using var deactivated = await admin.PostAsync(UserApi.Deactivate(manager.Id));
        using var browser = await CreateBrowserAsync();

        // Act
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        deactivated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.Revoked");
    }

    [Fact]
    public async Task Reassigning_the_user_to_another_team_revokes_their_link()
    {
        // Arrange
        var (_, interviewer, issued) = await IssueAsManagerAsync();
        var otherTeam = await Host.CreateTeamAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        using var reassigned = await admin.ReassignAsync(interviewer.Id, otherTeam.Id);
        using var browser = await CreateBrowserAsync();

        // Act
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        reassigned.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.Revoked");
        using var login = await browser.LoginAsync(interviewer.Email, interviewer.Password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Redemption_requires_the_antiforgery_header()
    {
        // Arrange
        var (_, issued) = await IssueForInterviewerAsync();
        using var browser = await CreateBrowserAsync();
        browser.SendsAntiforgeryHeader = false;

        // Act
        using var redeemed = await browser.RedeemResetLinkAsync(issued.Token);

        // Assert
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Antiforgery.InvalidToken");
    }

    [Fact]
    public async Task Redemption_is_rate_limited()
    {
        // Arrange
        using var browser = await CreateBrowserAsync();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var allowed = await browser.RedeemResetLinkAsync("not-a-real-reset-token");
        }

        // Act
        using var limited = await browser.RedeemResetLinkAsync("not-a-real-reset-token");

        // Assert
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private async Task<(TestUser Interviewer, IssuedResetLinkView Issued)> IssueForInterviewerAsync()
    {
        var (_, interviewer, issued) = await IssueAsManagerAsync();
        return (interviewer, issued);
    }

    private async Task<(TestUser Manager, TestUser Interviewer, IssuedResetLinkView Issued)> IssueAsManagerAsync()
    {
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(managerStaff.User.TeamId!.Value));
        var issued = await (await manager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync();
        return (managerStaff.User, interviewer, issued);
    }

    private async Task DeactivateAsync(UserId userId)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var user = await dbContext.Users.SingleAsync(user => user.Id == userId, CancellationToken);
        user.Deactivate();
        await dbContext.SaveChangesAsync(CancellationToken);
    }
}
