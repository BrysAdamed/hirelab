using System.Net;
using System.Net.Http.Json;
using HireLab.Api.Auth;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.Net.Http.Headers;

namespace HireLab.Api.Tests.Auth;

public sealed class StaffSessionTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly Uri _probe = new(AuthProbeModule.StaffPath, UriKind.Relative);
    private static readonly Uri _currentUser = new(AuthProbeModule.CurrentUserPath, UriKind.Relative);
    private static readonly TimeSpan _pastValidationInterval = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1);

    [Theory]
    [InlineData(StaffRole.Admin)]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Staff_endpoint_sees_the_signed_in_user_of_every_role(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        using var client = CreateClient(staff);

        // Act
        var currentUser = await client.GetFromJsonAsync<CurrentUserProbe>(_currentUser, CancellationToken);

        // Assert
        currentUser.ShouldBe(new CurrentUserProbe(true, staff.User.Id.Value, role, staff.User.TeamId?.Value));
    }

    [Fact]
    public async Task Session_survives_security_stamp_validation_when_nothing_changed()
    {
        // Arrange
        var session = new StaffSession(Host, await SignInAsAsync(StaffRole.Interviewer));
        Host.Time.Advance(_pastValidationInterval);

        // Act
        var status = await session.GetAsync(_probe);

        // Assert
        status.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Changing_the_security_stamp_ends_the_session_within_the_validation_window()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var session = new StaffSession(Host, staff);
        await Host.ChangeSecurityStampAsync(staff.User.Id);

        // Act
        var withinWindow = await session.GetAsync(_probe);
        Host.Time.Advance(_pastValidationInterval);
        var afterWindow = await session.GetAsync(_probe);

        // Assert
        withinWindow.ShouldBe(HttpStatusCode.NoContent);
        afterWindow.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivated_user_loses_the_session_at_the_next_validation()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var user = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).Inactive());
        var session = new StaffSession(Host, new SignedInStaff(user, await Host.SignInAsync(user)));
        Host.Time.Advance(_pastValidationInterval);

        // Act
        var status = await session.GetAsync(_probe);

        // Assert
        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_ends_after_eight_idle_hours()
    {
        // Arrange
        var session = new StaffSession(Host, await SignInAsAsync(StaffRole.Interviewer));
        Host.Time.Advance(StaffSessionLifetime.IdleTimeout + TimeSpan.FromSeconds(1));

        // Act
        var status = await session.GetAsync(_probe);

        // Assert
        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Active_session_ends_seven_days_after_sign_in()
    {
        // Arrange
        var session = new StaffSession(Host, await SignInAsAsync(StaffRole.Interviewer));
        var step = TimeSpan.FromHours(6);
        var steps = (int)(StaffSessionLifetime.AbsoluteLifetime / step);

        // Act
        var statusesBeforeCap = new List<HttpStatusCode>();
        for (var i = 1; i < steps; i++)
        {
            Host.Time.Advance(step);
            statusesBeforeCap.Add(await session.GetAsync(_probe));
        }

        Host.Time.Advance(step);
        var statusAtCap = await session.GetAsync(_probe);

        // Assert
        statusesBeforeCap.ShouldAllBe(status => status == HttpStatusCode.NoContent);
        statusAtCap.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed class StaffSession(HireLabTestHost host, SignedInStaff staff)
    {
        private SetCookieHeaderValue _cookie = staff.Cookies.Single(cookie => cookie.Name == AuthCookies.Staff);

        public async Task<HttpStatusCode> GetAsync(Uri uri)
        {
            using var client = host.CreateClient([_cookie]);
            using var response = await client.GetAsync(uri, TestContext.Current.CancellationToken);

            if (response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookies)
                && SetCookieHeaderValue.ParseList(setCookies.ToList()).FirstOrDefault(cookie => cookie.Name == AuthCookies.Staff) is { } renewed)
            {
                _cookie = renewed;
            }

            return response.StatusCode;
        }
    }
}
