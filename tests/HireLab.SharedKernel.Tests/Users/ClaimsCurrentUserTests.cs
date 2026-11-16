using System.Security.Claims;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Http;

namespace HireLab.SharedKernel.Tests.Users;

public sealed class ClaimsCurrentUserTests
{
    private static readonly UserId _userId = new(Guid.Parse("0190a5c4-7e3d-7b8a-9f21-3c4d5e6f7a8b"));
    private static readonly TeamId _teamId = new(Guid.Parse("0190a5c4-7e3d-7b8a-9f21-000000000001"));

    [Fact]
    public void Reads_the_id_role_and_team_from_the_claims()
    {
        // Arrange
        var currentUser = CurrentUserOf(Authenticated(
            new Claim(StaffClaimTypes.UserId, _userId.ToString()),
            new Claim(StaffClaimTypes.Role, nameof(StaffRole.Manager)),
            new Claim(StaffClaimTypes.TeamId, _teamId.ToString())));

        // Act and assert
        currentUser.IsAuthenticated.ShouldBeTrue();
        currentUser.Id.ShouldBe(_userId);
        currentUser.Role.ShouldBe(StaffRole.Manager);
        currentUser.TeamId.ShouldBe(_teamId);
    }

    [Fact]
    public void Admin_has_no_team()
    {
        // Arrange
        var currentUser = CurrentUserOf(Authenticated(
            new Claim(StaffClaimTypes.UserId, _userId.ToString()),
            new Claim(StaffClaimTypes.Role, nameof(StaffRole.Admin))));

        // Act and assert
        currentUser.Role.ShouldBe(StaffRole.Admin);
        currentUser.TeamId.ShouldBeNull();
    }

    [Fact]
    public void Anonymous_request_has_no_current_user()
    {
        // Arrange
        var currentUser = CurrentUserOf(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act and assert
        currentUser.IsAuthenticated.ShouldBeFalse();
        currentUser.TeamId.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => currentUser.Id);
        Should.Throw<InvalidOperationException>(() => currentUser.Role);
    }

    [Theory]
    [InlineData("Candidate")]
    [InlineData("42")]
    [InlineData(null)]
    public void Principal_without_a_staff_role_is_not_a_current_user(string? role)
    {
        // Arrange
        List<Claim> claims = [new Claim(StaffClaimTypes.UserId, _userId.ToString())];
        if (role is not null)
        {
            claims.Add(new Claim(StaffClaimTypes.Role, role));
        }

        var currentUser = CurrentUserOf(Authenticated([.. claims]));

        // Act and assert
        currentUser.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public void No_http_context_means_no_current_user()
    {
        // Arrange
        var currentUser = new ClaimsCurrentUser(new HttpContextAccessor());

        // Act and assert
        currentUser.IsAuthenticated.ShouldBeFalse();
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) => new(new ClaimsIdentity(claims, "Test"));

    private static ClaimsCurrentUser CurrentUserOf(ClaimsPrincipal principal) =>
        new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
}
