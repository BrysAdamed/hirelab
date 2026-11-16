using System.Net;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.Modules.Identity.Tests.Invites;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Bootstrap;

public sealed class AdminBootstrapTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Issues_an_admin_invite_and_logs_its_link_when_no_admin_exists()
    {
        // Act
        var link = await MigrationJob.RunAsync(Host);

        // Assert
        link.ShouldNotBeNull().ShouldStartWith($"{MigrationJob.PublicBaseUrl}/invite#");
        var invite = (await AdminInvitesAsync()).ShouldHaveSingleItem();
        invite.Email.ShouldBe(MigrationJob.AdminEmail);
        invite.InvitedBy.ShouldBeNull();
        invite.ExpiresAt.ShouldBe(Host.Time.GetUtcNow() + TimeSpan.FromHours(24));
        invite.StateAt(Host.Time.GetUtcNow()).ShouldBe(OneTimeLinkState.Pending);
    }

    [Fact]
    public async Task Issues_no_invite_when_an_admin_exists()
    {
        // Arrange
        await Host.CreateUserAsync(UserBuilder.Admin());

        // Act
        var link = await MigrationJob.RunAsync(Host);

        // Assert
        link.ShouldBeNull();
        (await AdminInvitesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Keeps_a_pending_invite_on_the_next_run()
    {
        // Arrange
        await MigrationJob.RunAsync(Host);
        Host.Time.Advance(TimeSpan.FromHours(23));

        // Act
        var link = await MigrationJob.RunAsync(Host);

        // Assert
        link.ShouldBeNull();
        (await AdminInvitesAsync()).ShouldHaveSingleItem().StateAt(Host.Time.GetUtcNow()).ShouldBe(OneTimeLinkState.Pending);
    }

    [Fact]
    public async Task Issues_a_new_link_when_the_pending_one_has_expired()
    {
        // Arrange
        await MigrationJob.RunAsync(Host);
        Host.Time.Advance(TimeSpan.FromHours(24));

        // Act
        var link = await MigrationJob.RunAsync(Host);

        // Assert
        link.ShouldNotBeNull();
        var states = (await AdminInvitesAsync()).Select(invite => invite.StateAt(Host.Time.GetUtcNow()));
        states.ShouldBe([OneTimeLinkState.Revoked, OneTimeLinkState.Pending], ignoreOrder: true);
    }

    [Fact]
    public async Task The_manual_option_revokes_the_pending_link_and_issues_a_new_one()
    {
        // Arrange
        var oldLink = (await MigrationJob.RunAsync(Host)).ShouldNotBeNull();

        // Act
        var newLink = await MigrationJob.RunAsync(Host, MigrationJob.DefaultSettings(issueNewLink: true));

        // Assert
        newLink.ShouldNotBeNull().ShouldNotBe(oldLink);
        using var invitee = await CreateBrowserAsync();
        using var redeemed = await invitee.RedeemAsync(TokenOf(oldLink));
        await redeemed.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.Revoked");
        (await invitee.PreviewAsync(TokenOf(newLink))).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_manual_option_revokes_a_pending_link_issued_for_another_email()
    {
        // Arrange
        await MigrationJob.RunAsync(Host, MigrationJob.DefaultSettings(adminEmail: "old.admin@example.com"));

        // Act
        await MigrationJob.RunAsync(Host, MigrationJob.DefaultSettings(issueNewLink: true));

        // Assert
        var invites = await AdminInvitesAsync();
        invites.Where(invite => invite.StateAt(Host.Time.GetUtcNow()) == OneTimeLinkState.Pending)
            .ShouldHaveSingleItem().Email.ShouldBe(MigrationJob.AdminEmail);
        invites.Count.ShouldBe(2);
    }

    [Fact]
    public async Task The_manual_option_never_creates_a_second_admin()
    {
        // Arrange
        await Host.CreateUserAsync(UserBuilder.Admin());
        await using var job = MigrationJob.Create(Host, MigrationJob.DefaultSettings(issueNewLink: true));

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(job.BootstrapAsync());

        // Assert
        exception.Message.ShouldContain("An admin already exists");
        job.AdminInviteLink().ShouldBeNull();
        (await AdminInvitesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Redeeming_the_link_creates_exactly_one_admin()
    {
        // Arrange
        var link = (await MigrationJob.RunAsync(Host)).ShouldNotBeNull();
        using var invitee = await CreateBrowserAsync();

        // Act
        using var redeemed = await invitee.RedeemAsync(TokenOf(link));

        // Assert
        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await redeemed.CurrentUserAsync();
        user.Role.ShouldBe(StaffRole.Admin);
        user.Team.ShouldBeNull();
        (await AdminCountAsync()).ShouldBe(1);
        (await MigrationJob.RunAsync(Host)).ShouldBeNull();
        (await AdminCountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Fails_clearly_when_the_admin_email_belongs_to_another_user()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        await Host.CreateUserAsync(UserBuilder.Manager(team.Id).WithEmail(MigrationJob.AdminEmail));
        await using var job = MigrationJob.Create(Host);

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(job.BootstrapAsync());

        // Assert
        exception.Message.ShouldContain("Invite.EmailInUse");
        exception.Message.ShouldNotContain(MigrationJob.AdminEmail);
        job.AdminInviteLink().ShouldBeNull();
    }

    private static string TokenOf(string link) => new Uri(link).Fragment.TrimStart('#');

    private async Task<List<Invite>> AdminInvitesAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().Invites
            .Where(invite => invite.Role == StaffRole.Admin)
            .ToListAsync(CancellationToken);
    }

    private async Task<int> AdminCountAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().Users
            .CountAsync(user => user.Role == StaffRole.Admin, CancellationToken);
    }
}
