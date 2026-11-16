using System.Net;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Invites;

public sealed class InviteReplacementTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string SharedEmail = "shared.invitee@example.com";

    [Fact]
    public async Task Manager_reissuing_an_invite_of_their_team_revokes_the_old_one()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var first = await (await manager.InviteInterviewerAsync(SharedEmail)).IssuedAsync();

        // Act
        using var response = await manager.InviteInterviewerAsync(SharedEmail);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await manager.ListAsync(InviteApi.InterviewerInvites)).Single(invite => invite.Id == first.Invite.Id).State.ShouldBe("Revoked");
    }

    [Fact]
    public async Task Manager_cannot_replace_the_admins_pending_manager_invite_for_the_same_email()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var managerInvite = await (await admin.InviteManagerToNewTeamAsync(SharedEmail)).IssuedAsync();
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.InviteInterviewerAsync(SharedEmail);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.PendingElsewhere");
        (await admin.ListAsync(InviteApi.ManagerInvites)).Single(invite => invite.Id == managerInvite.Invite.Id).State.ShouldBe("Pending");
        (await manager.ListAsync(InviteApi.InterviewerInvites)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Manager_cannot_replace_another_teams_pending_invite_for_the_same_email()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        await (await manager.InviteInterviewerAsync(SharedEmail)).IssuedAsync();
        using var otherManager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await otherManager.InviteInterviewerAsync(SharedEmail);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.PendingElsewhere");
        (await manager.ListAsync(InviteApi.InterviewerInvites)).Single().State.ShouldBe("Pending");
    }

    [Fact]
    public async Task Admin_cannot_replace_a_managers_pending_interviewer_invite_for_the_same_email()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        await (await manager.InviteInterviewerAsync(SharedEmail)).IssuedAsync();
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var response = await admin.InviteManagerToNewTeamAsync(SharedEmail);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.PendingElsewhere");
        (await manager.ListAsync(InviteApi.InterviewerInvites)).Single().State.ShouldBe("Pending");
    }

    [Fact]
    public async Task An_expired_invite_from_another_team_is_replaced()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using (var manager = await CreateBrowserAsync(managerStaff))
        {
            await (await manager.InviteInterviewerAsync(SharedEmail)).IssuedAsync();
        }

        Host.Time.Advance(TimeSpan.FromHours(72));
        using var otherManager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await otherManager.InviteInterviewerAsync(SharedEmail);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var managerLater = await CreateBrowserAsync(new SignedInStaff(managerStaff.User, await Host.SignInAsync(managerStaff.User)));
        (await managerLater.ListAsync(InviteApi.InterviewerInvites)).Single().State.ShouldBe("Revoked");
    }

    [Fact]
    public async Task An_invite_issued_for_the_same_email_by_a_concurrent_request_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        Host.ConcurrentChange.BeforeNextSave<Invite>(EntityState.Added, () => IssueConcurrentlyAsync(managerStaff));

        // Act
        using var response = await manager.InviteInterviewerAsync(SharedEmail);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.IssuedMeanwhile");
        (await manager.ListAsync(InviteApi.InterviewerInvites)).ShouldHaveSingleItem().State.ShouldBe("Pending");
    }

    [Fact]
    public async Task Replacing_an_invite_that_a_concurrent_request_replaced_first_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        await (await manager.InviteInterviewerAsync(SharedEmail)).IssuedAsync();
        Host.ConcurrentChange.BeforeNextSave<Invite>(EntityState.Modified, () => IssueConcurrentlyAsync(managerStaff));

        // Act
        using var response = await manager.InviteInterviewerAsync(SharedEmail);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invite.IssuedMeanwhile");
        var states = (await manager.ListAsync(InviteApi.InterviewerInvites)).Select(invite => invite.State);
        states.ShouldBe(["Revoked", "Pending"], ignoreOrder: true);
    }

    [Fact]
    public async Task Revoking_an_invite_that_a_concurrent_request_changed_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        var issued = await (await manager.InviteInterviewerAsync(SharedEmail)).IssuedAsync();
        Host.ConcurrentChange.BeforeNextSave<Invite>(EntityState.Modified, () => RevokeDirectlyAsync(issued.Invite.Id));

        // Act
        using var response = await manager.PostAsync(InviteApi.RevokeInterviewerInvite(issued.Invite.Id));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.ChangedMeanwhile");
        (await manager.ListAsync(InviteApi.InterviewerInvites)).Single().State.ShouldBe("Revoked");
    }

    private async Task IssueConcurrentlyAsync(SignedInStaff manager)
    {
        using var concurrent = await CreateBrowserAsync(manager);
        await (await concurrent.InviteInterviewerAsync(SharedEmail)).IssuedAsync();
    }

    private async Task RevokeDirectlyAsync(Guid inviteId)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var invite = await dbContext.Invites.SingleAsync(invite => invite.Id == new InviteId(inviteId), CancellationToken);
        invite.Revoke(Host.Time.GetUtcNow());
        await dbContext.SaveChangesAsync(CancellationToken);
    }
}
