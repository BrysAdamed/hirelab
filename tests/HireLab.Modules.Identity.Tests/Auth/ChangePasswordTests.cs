using System.Net;
using HireLab.Modules.Identity.Tests.Invites;
using HireLab.Modules.Identity.Tests.ResetLinks;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Tests.Auth;

public sealed class ChangePasswordTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string NewPassword = "changed-password-not-a-secret";
    private static readonly TimeSpan _pastValidationInterval = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Changing_the_password_keeps_this_session_and_ends_the_others()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        using var thisSession = await CreateBrowserAsync(staff);
        using var otherSession = await CreateBrowserAsync(staff);

        // Act
        using var changed = await thisSession.ChangePasswordAsync(staff.User.Password, NewPassword);
        Host.Time.Advance(_pastValidationInterval);

        // Assert
        changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var thisMe = await thisSession.GetAsync(AuthApi.Me);
        thisMe.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var logout = await thisSession.PostAsync(AuthApi.Logout);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var otherMe = await otherSession.GetAsync(AuthApi.Me);
        otherMe.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_new_password_replaces_the_old_one_at_login()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        using var browser = await CreateBrowserAsync(staff);
        using var anonymous = await CreateBrowserAsync();

        // Act
        using var changed = await browser.ChangePasswordAsync(staff.User.Password, NewPassword);

        // Assert
        changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var oldPassword = await anonymous.LoginAsync(staff.User.Email, staff.User.Password);
        oldPassword.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var newPassword = await anonymous.LoginAsync(staff.User.Email, NewPassword);
        newPassword.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_wrong_current_password_is_refused_and_changes_nothing()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Admin);
        using var browser = await CreateBrowserAsync(staff);
        using var anonymous = await CreateBrowserAsync();

        // Act
        using var response = await browser.ChangePasswordAsync("wrong-password-not-a-secret", NewPassword);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Auth.WrongCurrentPassword");
        using var login = await anonymous.LoginAsync(staff.User.Email, staff.User.Password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_new_password_that_breaks_the_policy_is_refused()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.ChangePasswordAsync(staff.User.Password, "short");

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Auth.PasswordRejected");
    }

    [Fact]
    public async Task Empty_passwords_are_a_validation_error()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.ChangePasswordAsync(string.Empty, string.Empty);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }
}
