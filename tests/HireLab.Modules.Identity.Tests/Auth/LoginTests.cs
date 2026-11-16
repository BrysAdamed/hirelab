using System.Net;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;

namespace HireLab.Modules.Identity.Tests.Auth;

public sealed class LoginTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string InvalidCredentials = "Auth.InvalidCredentials";
    private const string StaffCookie = "__Host-hirelab-staff";

    [Fact]
    public async Task Login_with_the_right_password_starts_a_session_and_returns_the_user()
    {
        // Arrange
        var team = await Host.CreateTeamAsync(new TeamBuilder().WithName("Fake Team Alpha"));
        var user = await Host.CreateUserAsync(UserBuilder.Manager(team.Id).WithEmail("jane.doe@example.com").WithDisplayName("Jane Doe"));
        using var browser = await CreateBrowserAsync();
        var anonymousToken = browser.Cookie(AntiforgeryTokens.CookieName);

        // Act
        using var response = await browser.LoginAsync("Jane.Doe@example.com", user.Password);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.CurrentUserAsync()).ShouldBe(new CurrentUser(
            user.Id.Value, "jane.doe@example.com", "Jane Doe", StaffRole.Manager, new CurrentUserTeam(team.Id.Value, "Fake Team Alpha")));
        browser.Cookie(StaffCookie).ShouldNotBeNull();
        browser.Cookie(AntiforgeryTokens.CookieName).ShouldNotBe(anonymousToken);

        using var me = await browser.GetAsync(AuthApi.Me);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Antiforgery_token_issued_at_login_is_accepted_on_the_next_unsafe_request()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var user = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var browser = await CreateBrowserAsync();
        using var login = await browser.LoginAsync(user.Email, user.Password);

        // Act
        using var logout = await browser.PostAsync(AuthApi.Logout);

        // Assert
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Wrong_password_is_refused_with_the_generic_message()
    {
        // Arrange
        var user = await Host.CreateUserAsync(UserBuilder.Admin());
        using var browser = await CreateBrowserAsync();

        // Act
        using var response = await browser.LoginAsync(user.Email, "wrong-password-not-a-secret");

        // Assert
        await ShouldBeInvalidCredentialsAsync(response);
        browser.Cookie(StaffCookie).ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_answer_as_a_wrong_password()
    {
        // Arrange
        var user = await Host.CreateUserAsync(UserBuilder.Admin());
        using var browser = await CreateBrowserAsync();
        using var wrongPassword = await browser.LoginAsync(user.Email, "wrong-password-not-a-secret");

        // Act
        using var unknownEmail = await browser.LoginAsync("nobody@example.com", "wrong-password-not-a-secret");

        // Assert
        var expected = await wrongPassword.ProblemAsync();
        var actual = await ShouldBeInvalidCredentialsAsync(unknownEmail);
        actual.Detail.ShouldBe(expected.Detail);
    }

    [Fact]
    public async Task Unknown_email_costs_a_password_hash_verification_like_a_wrong_password()
    {
        // Arrange
        var user = await Host.CreateUserAsync(UserBuilder.Admin());
        using var browser = await CreateBrowserAsync();
        using var wrongPassword = await browser.LoginAsync(user.Email, "wrong-password-not-a-secret");
        var forWrongPassword = Host.PasswordHashes.Verifications;
        Host.PasswordHashes.Reset();

        // Act
        using var unknownEmail = await browser.LoginAsync("nobody@example.com", "wrong-password-not-a-secret");

        // Assert
        await ShouldBeInvalidCredentialsAsync(unknownEmail);
        forWrongPassword.ShouldBe(1);
        Host.PasswordHashes.Verifications.ShouldBe(1);
    }

    [Fact]
    public async Task A_locked_out_account_costs_a_password_hash_verification_like_a_wrong_password()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var user = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var browser = await CreateBrowserAsync();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var failed = await browser.LoginAsync(user.Email, "wrong-password-not-a-secret");
        }

        Host.PasswordHashes.Reset();
        using var lockingAttempt = await browser.LoginAsync(user.Email, "wrong-password-not-a-secret");
        var forLockingAttempt = Host.PasswordHashes.Verifications;
        Host.PasswordHashes.Reset();

        // Act
        using var response = await browser.LoginAsync(user.Email, user.Password);

        // Assert
        await ShouldBeInvalidCredentialsAsync(response);
        forLockingAttempt.ShouldBe(1);
        Host.PasswordHashes.Verifications.ShouldBe(1);
    }

    [Fact]
    public async Task An_inactive_account_costs_a_password_hash_verification_like_a_wrong_password()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var user = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).Inactive());
        using var browser = await CreateBrowserAsync();

        // Act
        using var response = await browser.LoginAsync(user.Email, user.Password);

        // Assert
        await ShouldBeInvalidCredentialsAsync(response);
        Host.PasswordHashes.Verifications.ShouldBe(1);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_so_even_the_right_one_is_refused()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var user = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        using var browser = await CreateBrowserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await browser.LoginAsync(user.Email, "wrong-password-not-a-secret");
            failed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        // Act
        using var response = await browser.LoginAsync(user.Email, user.Password);

        // Assert
        await ShouldBeInvalidCredentialsAsync(response);
        browser.Cookie(StaffCookie).ShouldBeNull();
    }

    [Fact]
    public async Task Inactive_user_is_refused_with_the_generic_message()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var user = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).Inactive());
        using var browser = await CreateBrowserAsync();

        // Act
        using var response = await browser.LoginAsync(user.Email, user.Password);

        // Assert
        await ShouldBeInvalidCredentialsAsync(response);
        browser.Cookie(StaffCookie).ShouldBeNull();
    }

    [Fact]
    public async Task Empty_email_and_password_are_a_validation_error()
    {
        // Arrange
        using var browser = await CreateBrowserAsync();

        // Act
        using var response = await browser.LoginAsync(string.Empty, string.Empty);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemAsync()).Code().ShouldBe("Validation.Failed");
    }

    [Fact]
    public async Task Login_without_the_antiforgery_header_is_refused()
    {
        // Arrange
        var user = await Host.CreateUserAsync(UserBuilder.Admin());
        using var browser = await CreateBrowserAsync();
        browser.SendsAntiforgeryHeader = false;

        // Act
        using var response = await browser.LoginAsync(user.Email, user.Password);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemAsync()).Code().ShouldBe("Antiforgery.InvalidToken");
        browser.Cookie(StaffCookie).ShouldBeNull();
    }

    [Fact]
    public async Task Login_without_any_antiforgery_token_is_refused()
    {
        // Arrange
        var user = await Host.CreateUserAsync(UserBuilder.Admin());
        using var browser = Host.CreateBrowser();

        // Act
        using var response = await browser.LoginAsync(user.Email, user.Password);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemAsync()).Code().ShouldBe("Antiforgery.InvalidToken");
    }

    [Fact]
    public async Task More_than_ten_attempts_a_minute_from_one_address_are_rate_limited()
    {
        // Arrange
        using var browser = await CreateBrowserAsync();
        using var otherBrowser = await CreateBrowserAsync();
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var allowed = await browser.LoginAsync("nobody@example.com", "wrong-password-not-a-secret");
            statuses.Add(allowed.StatusCode);
        }

        // Act
        using var limited = await browser.LoginAsync("nobody@example.com", "wrong-password-not-a-secret");
        using var otherAddress = await otherBrowser.LoginAsync("nobody@example.com", "wrong-password-not-a-secret");

        // Assert
        statuses.ShouldAllBe(status => status == HttpStatusCode.BadRequest);
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.ShouldNotBeNull();
        (await limited.ProblemAsync()).Status.ShouldBe((int)HttpStatusCode.TooManyRequests);
        otherAddress.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task<Microsoft.AspNetCore.Mvc.ProblemDetails> ShouldBeInvalidCredentialsAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ProblemAsync();
        problem.Code().ShouldBe(InvalidCredentials);
        problem.Detail.ShouldNotBeNullOrWhiteSpace();
        return problem;
    }
}
