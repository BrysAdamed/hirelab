using System.Net;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Tests.Auth;

public sealed class SessionEndpointsTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string StaffCookie = "__Host-hirelab-staff";

    [Theory]
    [InlineData(StaffRole.Admin)]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Me_returns_the_signed_in_user_of_every_role(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.GetAsync(AuthApi.Me);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var currentUser = await response.CurrentUserAsync();
        currentUser.Id.ShouldBe(staff.User.Id.Value);
        currentUser.Email.ShouldBe(staff.User.Email);
        currentUser.DisplayName.ShouldBe(staff.User.DisplayName);
        currentUser.Role.ShouldBe(role);
        (currentUser.Team?.Id).ShouldBe(staff.User.TeamId?.Value);
        (currentUser.Team is null).ShouldBe(role == StaffRole.Admin);
    }

    [Fact]
    public async Task Me_is_unauthorised_without_a_session()
    {
        // Arrange
        using var browser = await CreateBrowserAsync();

        // Act
        using var response = await browser.GetAsync(AuthApi.Me);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ends_the_session_and_issues_an_anonymous_antiforgery_token()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        using var browser = await CreateBrowserAsync(staff);
        var signedInToken = browser.Cookie(AntiforgeryTokens.CookieName);

        // Act
        using var response = await browser.PostAsync(AuthApi.Logout);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        browser.Cookie(StaffCookie).ShouldBeNull();
        browser.Cookie(AntiforgeryTokens.CookieName).ShouldNotBe(signedInToken);

        using var me = await browser.GetAsync(AuthApi.Me);
        me.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var login = await browser.LoginAsync(staff.User.Email, staff.User.Password);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_without_the_antiforgery_header_is_refused_and_keeps_the_session()
    {
        // Arrange
        using var browser = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Interviewer));
        browser.SendsAntiforgeryHeader = false;

        // Act
        using var response = await browser.PostAsync(AuthApi.Logout);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemAsync()).Code().ShouldBe("Antiforgery.InvalidToken");
        using var me = await browser.GetAsync(AuthApi.Me);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Antiforgery_token_issued_before_login_is_refused_after_login()
    {
        // Arrange
        var user = await Host.CreateUserAsync(Testing.Staff.UserBuilder.Admin());
        using var browser = await CreateBrowserAsync();
        var anonymousToken = browser.Cookie(AntiforgeryTokens.CookieName).ShouldNotBeNull();
        using var login = await browser.LoginAsync(user.Email, user.Password);
        using var request = new HttpRequestMessage(HttpMethod.Post, AuthApi.Logout);
        request.Headers.Add(AntiforgeryTokens.HeaderName, anonymousToken);
        browser.SendsAntiforgeryHeader = false;

        // Act
        using var response = await browser.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
