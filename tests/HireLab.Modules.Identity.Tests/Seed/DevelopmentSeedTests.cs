using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Seed;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Seed;

public sealed class DevelopmentSeedTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public void Seed_data_is_obviously_fake()
    {
        // Act
        var data = DevelopmentSeed.Load();

        // Assert
        var users = data.Teams.SelectMany(team => team.Interviewers.Append(team.Manager)).Append(data.Admin).ToList();
        users.ShouldAllBe(user => user.Email.EndsWith("@example.com", StringComparison.Ordinal));
        data.Password.Length.ShouldBeGreaterThanOrEqualTo(12);
    }

    [Fact]
    public async Task Seed_creates_the_admin_and_a_sample_team_with_a_manager_and_an_interviewer()
    {
        // Act
        await SeedAsync();

        // Assert
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var team = await dbContext.Teams.SingleAsync(CancellationToken);
        var users = await dbContext.Users.ToListAsync(CancellationToken);

        users.Select(user => (user.Role, user.TeamId)).ShouldBe(
            [(StaffRole.Admin, null), (StaffRole.Manager, team.Id), (StaffRole.Interviewer, team.Id)],
            ignoreOrder: true);
        users.ShouldAllBe(user => user.IsActive);
    }

    [Fact]
    public async Task Seeded_users_log_in_with_the_seed_password()
    {
        // Arrange
        var data = DevelopmentSeed.Load();
        await SeedAsync();

        // Act and assert
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        foreach (var email in new[] { data.Admin.Email, data.Teams[0].Manager.Email, data.Teams[0].Interviewers[0].Email })
        {
            var user = (await userManager.FindByEmailAsync(email)).ShouldNotBeNull();
            (await userManager.CheckPasswordAsync(user, data.Password)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        // Arrange
        await SeedAsync();

        // Act
        await SeedAsync();

        // Assert
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await dbContext.Teams.CountAsync(CancellationToken)).ShouldBe(1);
        (await dbContext.Users.CountAsync(CancellationToken)).ShouldBe(3);
    }

    private async Task SeedAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        await new IdentityModule().SeedDevelopmentDataAsync(scope.ServiceProvider, CancellationToken);
    }
}
