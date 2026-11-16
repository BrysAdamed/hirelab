using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace HireLab.Testing;

public partial class HireLabTestHost
{
    /// <summary>Persists a team built by <paramref name="builder"/>, or a default one.</summary>
    public async Task<TestTeam> CreateTeamAsync(TeamBuilder? builder = null)
    {
        var team = (builder ?? new TeamBuilder()).Build();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

        return new TestTeam(team.Id, team.Name);
    }

    /// <summary>Persists a user built by <paramref name="builder"/> through the real <c>UserManager</c>.</summary>
    public async Task<TestUser> CreateUserAsync(UserBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var user = builder.Build();

        await using var scope = Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var result = await userManager.CreateAsync(user, builder.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Creating the test user failed: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }

        return new TestUser(user.Id, user.Email!, user.DisplayName, user.Role, user.TeamId, builder.Password);
    }

    /// <summary>Signs <paramref name="user"/> in with the staff cookie scheme and returns the issued cookies.</summary>
    public Task<IReadOnlyList<SetCookieHeaderValue>> SignInAsync(TestUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return SignInAsync(TestSignIn.StaffUri(user.Id));
    }

    /// <summary>Creates a service scope that runs as <paramref name="user"/>, with their team query filter.</summary>
    public StaffScope CreateScopeAs(TestUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new StaffScope(Services, user);
    }

    /// <summary>Gives a user a new security stamp, as deactivation, a role or team change and a password reset do.</summary>
    public async Task ChangeSecurityStampAsync(UserId userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"The test user {userId} does not exist.");

        var result = await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Changing the security stamp failed: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
    }

    /// <summary>
    /// Creates a user in <paramref name="role"/> (and a team for a manager or an interviewer) and signs them in with the
    /// staff cookie scheme.
    /// </summary>
    public async Task<SignedInStaff> SignInAsAsync(StaffRole role)
    {
        var builder = role switch
        {
            StaffRole.Admin => UserBuilder.Admin(),
            StaffRole.Manager => UserBuilder.Manager((await CreateTeamAsync()).Id),
            StaffRole.Interviewer => UserBuilder.Interviewer((await CreateTeamAsync()).Id),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };

        var user = await CreateUserAsync(builder);
        return new SignedInStaff(user, await SignInAsync(user));
    }
}
