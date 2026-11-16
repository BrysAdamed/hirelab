using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Persistence.Configurations;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HireLab.Modules.Identity.Tests.Domain;

public sealed class UserRulesTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task There_is_at_most_one_admin()
    {
        // Arrange
        await Host.CreateUserAsync(UserBuilder.Admin());

        // Act
        var violation = await ConstraintViolationOf(() => Host.CreateUserAsync(UserBuilder.Admin()));

        // Assert
        violation.ShouldBe(UserConfiguration.SingleAdminIndex);
    }

    [Fact]
    public async Task A_team_has_at_most_one_active_manager()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id));

        // Act
        var violation = await ConstraintViolationOf(() => Host.CreateUserAsync(UserBuilder.Manager(team.Id)));

        // Assert
        violation.ShouldBe(UserConfiguration.OneActiveManagerPerTeamIndex);
    }

    [Fact]
    public async Task A_team_can_get_a_new_manager_once_the_old_one_is_deactivated()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id).Inactive());

        // Act
        var newManager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));

        // Assert
        newManager.TeamId.ShouldBe(team.Id);
    }

    [Fact]
    public async Task A_deactivated_manager_cannot_be_reactivated_while_the_team_has_another_active_manager()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var oldManager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id).Inactive());
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id));

        // Act
        var violation = await ConstraintViolationOf(() => UpdateAsync(oldManager.Id, user => user.Reactivate()));

        // Assert
        violation.ShouldBe(UserConfiguration.OneActiveManagerPerTeamIndex);
    }

    [Fact]
    public async Task Each_team_has_its_own_manager_and_any_number_of_interviewers()
    {
        // Arrange
        var first = await Host.CreateTeamAsync();
        var second = await Host.CreateTeamAsync();

        // Act
        await Host.CreateUserAsync(UserBuilder.Manager(first.Id));
        await Host.CreateUserAsync(UserBuilder.Manager(second.Id));
        await Host.CreateUserAsync(UserBuilder.Interviewer(first.Id));
        await Host.CreateUserAsync(UserBuilder.Interviewer(first.Id));

        // Assert
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await dbContext.Users.CountAsync(user => user.TeamId == first.Id, CancellationToken)).ShouldBe(3);
        (await dbContext.Users.CountAsync(user => user.TeamId == second.Id, CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Only_the_admin_has_no_team()
    {
        // Arrange
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var team = await Host.CreateTeamAsync();
        var admin = await Host.CreateUserAsync(UserBuilder.Admin());

        // Act
        var violation = await ConstraintViolationOf(() => dbContext.Database.ExecuteSqlAsync(
            $"UPDATE identity.users SET team_id = {team.Id.Value} WHERE id = {admin.Id.Value}",
            CancellationToken));

        // Assert
        violation.ShouldBe(UserConfiguration.TeamMatchesRoleConstraint);
    }

    [Fact]
    public async Task Rows_get_audit_timestamps_from_the_clock()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();

        // Act
        var stored = await dbContext.Teams
            .Where(row => row.Id == team.Id)
            .Select(row => new
            {
                CreatedAt = EF.Property<DateTimeOffset>(row, AuditTimestamps.CreatedAt),
                UpdatedAt = EF.Property<DateTimeOffset>(row, AuditTimestamps.UpdatedAt),
            })
            .SingleAsync(CancellationToken);

        // Assert
        stored.CreatedAt.ShouldBe(Host.Time.GetUtcNow());
        stored.UpdatedAt.ShouldBe(Host.Time.GetUtcNow());
    }

    private async Task UpdateAsync(UserId userId, Action<User> change)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        change(user.ShouldNotBeNull());
        await userManager.UpdateAsync(user);
    }

    private static async Task<string?> ConstraintViolationOf(Func<Task> action)
    {
        var exception = await Should.ThrowAsync<Exception>(action);
        var postgres = exception as PostgresException ?? exception.InnerException as PostgresException;

        return postgres.ShouldNotBeNull().ConstraintName;
    }
}
