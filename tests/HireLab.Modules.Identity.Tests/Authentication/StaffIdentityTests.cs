using System.Security.Claims;
using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Authentication;

public sealed class StaffIdentityTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData("eleven char")]
    [InlineData("")]
    public async Task Passwords_shorter_than_12_characters_are_rejected(string password)
    {
        // Act
        var errors = await PasswordErrorsAsync(password);

        // Assert
        errors.ShouldContain(nameof(IdentityErrorDescriber.PasswordTooShort));
    }

    [Theory]
    [InlineData("twelve chars")]
    [InlineData("aaaaaaaaaaaa")]
    public async Task Passwords_of_12_characters_need_no_composition(string password)
    {
        // Act
        var errors = await PasswordErrorsAsync(password);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Five_failed_attempts_lock_the_user_out_for_15_minutes()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var created = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = (await userManager.FindByIdAsync(created.Id.ToString())).ShouldNotBeNull();

        // Act
        for (var attempt = 1; attempt < 5; attempt++)
        {
            await userManager.AccessFailedAsync(user);
        }

        var lockedAfterFour = await userManager.IsLockedOutAsync(user);
        var before = DateTimeOffset.UtcNow;
        await userManager.AccessFailedAsync(user);
        var after = DateTimeOffset.UtcNow;

        // Assert
        lockedAfterFour.ShouldBeFalse();
        (await userManager.IsLockedOutAsync(user)).ShouldBeTrue();
        var lockoutEnd = (await userManager.GetLockoutEndDateAsync(user)).ShouldNotBeNull();
        lockoutEnd.ShouldBeInRange(before.AddMinutes(15), after.AddMinutes(15));
    }

    [Fact]
    public async Task Principal_carries_the_id_role_and_team()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));

        // Act
        var principal = await CreatePrincipalAsync(manager.Id);

        // Assert
        principal.FindFirstValue(StaffClaimTypes.UserId).ShouldBe(manager.Id.ToString());
        principal.FindFirstValue(StaffClaimTypes.Role).ShouldBe(nameof(StaffRole.Manager));
        principal.FindFirstValue(StaffClaimTypes.TeamId).ShouldBe(team.Id.ToString());
        principal.IsInRole(nameof(StaffRole.Manager)).ShouldBeTrue();
    }

    [Fact]
    public async Task Admin_principal_has_no_team()
    {
        // Arrange
        var admin = await Host.CreateUserAsync(UserBuilder.Admin());

        // Act
        var principal = await CreatePrincipalAsync(admin.Id);

        // Assert
        principal.FindFirstValue(StaffClaimTypes.Role).ShouldBe(nameof(StaffRole.Admin));
        principal.FindFirst(StaffClaimTypes.TeamId).ShouldBeNull();
    }

    [Fact]
    public async Task Inactive_users_cannot_sign_in()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var active = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        var inactive = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).Inactive());
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<User>>();

        // Act
        var activeCanSignIn = await signInManager.CanSignInAsync((await userManager.FindByIdAsync(active.Id.ToString()))!);
        var inactiveCanSignIn = await signInManager.CanSignInAsync((await userManager.FindByIdAsync(inactive.Id.ToString()))!);

        // Assert
        activeCanSignIn.ShouldBeTrue();
        inactiveCanSignIn.ShouldBeFalse();
    }

    [Fact]
    public async Task Users_are_found_by_their_email()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var created = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id).WithEmail("Jane.Doe@Example.com").WithDisplayName("Jane Doe"));
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Act
        var user = await userManager.FindByNameAsync("jane.doe@example.com");

        // Assert
        user.ShouldNotBeNull().Id.ShouldBe(created.Id);
        user.DisplayName.ShouldBe("Jane Doe");
        user.IsActive.ShouldBeTrue();
    }

    private async Task<IReadOnlyList<string>> PasswordErrorsAsync(string password)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = User.CreateInterviewer("jane.doe@example.com", "Jane Doe", TeamId.New());

        var errors = new List<string>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            errors.AddRange(result.Errors.Select(error => error.Code));
        }

        return errors;
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(UserId userId)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var factory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();

        return await factory.CreateAsync((await userManager.FindByIdAsync(userId.ToString())).ShouldNotBeNull());
    }
}
