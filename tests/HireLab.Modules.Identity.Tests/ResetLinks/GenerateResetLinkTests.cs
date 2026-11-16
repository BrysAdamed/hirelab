using System.Net;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Tests.Invites;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.ResetLinks;

public sealed class GenerateResetLinkTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Admin_generates_a_24_hour_link_for_a_manager_and_only_its_hash_is_stored()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var team = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id));

        // Act
        var issued = await (await admin.GenerateForManagerAsync(manager.Id.Value)).IssuedAsync();

        // Assert
        issued.UserId.ShouldBe(manager.Id.Value);
        issued.LinkPath.ShouldStartWith("/reset#");
        issued.ExpiresAt.ShouldBe(Host.Time.GetUtcNow() + TimeSpan.FromHours(24));
        var link = await SingleLinkAsync();
        link.UserId.ShouldBe(manager.Id);
        link.TokenHash.ShouldBe(OneTimeToken.HashOf(issued.Token));
        link.TokenHash.ShouldNotContain(issued.Token);
    }

    [Fact]
    public async Task Manager_generates_a_link_for_an_interviewer_of_their_team()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(managerStaff.User.TeamId!.Value));

        // Act
        var issued = await (await manager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync();

        // Assert
        issued.UserId.ShouldBe(interviewer.Id.Value);
        (await SingleLinkAsync()).IssuedBy.ShouldBe(managerStaff.User.Id);
    }

    [Fact]
    public async Task Manager_cannot_generate_a_link_for_another_team_or_for_a_manager()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        var otherTeam = await Host.CreateTeamAsync();
        var otherInterviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(otherTeam.Id));
        var otherManager = await Host.CreateUserAsync(UserBuilder.Manager(otherTeam.Id));

        // Act
        using var forOtherTeam = await manager.GenerateForInterviewerAsync(otherInterviewer.Id.Value);
        using var forManager = await manager.GenerateForInterviewerAsync(otherManager.Id.Value);
        using var forThemselves = await manager.GenerateForInterviewerAsync(managerStaff.User.Id.Value);

        // Assert
        await forOtherTeam.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.UserNotFound");
        await forManager.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.UserNotFound");
        await forThemselves.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.UserNotFound");
        (await LinkCountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Admin_cannot_generate_a_link_for_an_interviewer_or_an_unknown_user()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var team = await Host.CreateTeamAsync();
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));

        // Act
        using var forInterviewer = await admin.GenerateForManagerAsync(interviewer.Id.Value);
        using var forUnknown = await admin.GenerateForManagerAsync(Guid.CreateVersion7());

        // Assert
        await forInterviewer.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.UserNotFound");
        await forUnknown.ShouldBeProblemAsync(HttpStatusCode.NotFound, "ResetLink.UserNotFound");
    }

    [Theory]
    [InlineData(StaffRole.Admin, "interviewers")]
    [InlineData(StaffRole.Manager, "managers")]
    [InlineData(StaffRole.Interviewer, "managers")]
    [InlineData(StaffRole.Interviewer, "interviewers")]
    public async Task Other_roles_are_refused_with_403(StaffRole role, string kind)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        using var browser = await CreateBrowserAsync(staff);
        var team = await Host.CreateTeamAsync();
        var target = await Host.CreateUserAsync(kind == "managers" ? UserBuilder.Manager(team.Id) : UserBuilder.Interviewer(team.Id));

        // Act
        using var response = await browser.PostAsync(new Uri($"/api/staff/reset-links/{kind}/{target.Id.Value}", UriKind.Relative));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_deactivated_user_gets_no_link()
    {
        // Arrange
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));
        var team = await Host.CreateTeamAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(team.Id).Inactive());

        // Act
        using var response = await admin.GenerateForManagerAsync(manager.Id.Value);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.UserInactive");
        (await LinkCountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_new_link_revokes_the_previous_unused_one()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(managerStaff.User.TeamId!.Value));
        var first = await (await manager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync();
        using var anonymous = await CreateBrowserAsync();

        // Act
        var second = await (await manager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync();

        // Assert
        using var superseded = await anonymous.RedeemResetLinkAsync(first.Token);
        await superseded.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.Revoked");
        using var current = await anonymous.PreviewResetLinkAsync(second.Token);
        current.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_link_generated_for_the_same_user_by_a_concurrent_request_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        using var concurrentManager = await CreateBrowserAsync(managerStaff);
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(managerStaff.User.TeamId!.Value));
        Host.ConcurrentChange.BeforeNextSave<ResetLink>(
            EntityState.Added,
            async () => await (await concurrentManager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync());

        // Act
        using var response = await manager.GenerateForInterviewerAsync(interviewer.Id.Value);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.IssuedMeanwhile");
        (await SingleLinkAsync()).StateAt(Host.Time.GetUtcNow()).ShouldBe(OneTimeLinkState.Pending);
    }

    [Fact]
    public async Task Replacing_a_link_that_a_concurrent_request_replaced_first_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        using var manager = await CreateBrowserAsync(managerStaff);
        using var concurrentManager = await CreateBrowserAsync(managerStaff);
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(managerStaff.User.TeamId!.Value));
        await (await manager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync();
        Host.ConcurrentChange.BeforeNextSave<ResetLink>(
            EntityState.Modified,
            async () => await (await concurrentManager.GenerateForInterviewerAsync(interviewer.Id.Value)).IssuedAsync());

        // Act
        using var response = await manager.GenerateForInterviewerAsync(interviewer.Id.Value);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "ResetLink.IssuedMeanwhile");
        (await LinkCountAsync()).ShouldBe(2);
    }

    private async Task<ResetLink> SingleLinkAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().ResetLinks.SingleAsync(CancellationToken);
    }

    private async Task<int> LinkCountAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>().ResetLinks.CountAsync(CancellationToken);
    }
}
