using System.Net;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.Modules.Positions.Tests.JobPositions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.Candidates;

public sealed class CandidateTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Manager_and_interviewer_add_a_candidate_to_an_open_position(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var positionId = await Host.CreateJobPositionAsync(staff.User.TeamId!.Value, new JobPositionBuilder().WithTitle("Fake Tester"));
        using var browser = await CreateBrowserAsync(staff);
        var input = CandidateApi.ValidInput();

        // Act
        var added = await browser.AddedAsync(positionId, input);

        // Assert
        added.Number.ShouldBe(1);
        var candidate = await browser.CandidateAsync(added.CandidateId);
        candidate.Id.ShouldBe(added.Id);
        candidate.PositionId.ShouldBe(positionId.Value);
        candidate.PositionTitle.ShouldBe("Fake Tester");
        candidate.PositionState.ShouldBe(JobPositionState.Open);
        candidate.Number.ShouldBe(1);
        candidate.DisplayName.ShouldBe("Jane Fakeperson");
        candidate.FullName.ShouldBe("Jane Fakeperson");
        candidate.Email.ShouldBe(input.Email);
        candidate.Phone.ShouldBe(input.Phone);
        candidate.Links.ShouldBe(input.Links!);
        candidate.IsAnonymised.ShouldBeFalse();
        candidate.CreatedAt.ShouldBe(Host.Time.GetUtcNow());
        (await browser.CandidatesAsync(positionId)).ShouldHaveSingleItem().ShouldBe(new PositionCandidateView(
            added.Id,
            1,
            "Jane Fakeperson",
            input.Email,
            input.Phone,
            IsAnonymised: false,
            Host.Time.GetUtcNow()));
    }

    [Fact]
    public async Task The_creator_is_recorded()
    {
        // Arrange
        var interviewerStaff = await SignInAsAsync(StaffRole.Interviewer);
        var positionId = await Host.CreateJobPositionAsync(interviewerStaff.User.TeamId!.Value);
        using var interviewer = await CreateBrowserAsync(interviewerStaff);

        // Act
        var added = await interviewer.AddedAsync(positionId, CandidateApi.ValidInput());

        // Assert
        await using var scope = Host.CreateScopeAs(interviewerStaff.User);
        var dbContext = scope.Services.GetRequiredService<PositionsDbContext>();
        var candidate = await dbContext.Candidates.SingleAsync(candidate => candidate.Id == added.CandidateId, CancellationToken);
        candidate.CreatedBy.ShouldBe(interviewerStaff.User.Id);
    }

    [Fact]
    public async Task Optional_details_may_be_blank_and_values_are_trimmed()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        var added = await manager.AddedAsync(positionId, new CandidateInput("  John Fakeman  ", "  ", "", [" https://profiles.example.com/john "]));
        var withoutLinks = await manager.AddedAsync(positionId, new CandidateInput("Joan Fakewoman", null, null, null));

        // Assert
        var candidate = await manager.CandidateAsync(added.CandidateId);
        candidate.FullName.ShouldBe("John Fakeman");
        candidate.Email.ShouldBeNull();
        candidate.Phone.ShouldBeNull();
        candidate.Links.ShouldBe(["https://profiles.example.com/john"]);
        (await manager.CandidateAsync(withoutLinks.CandidateId)).Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task Numbers_are_sequential_per_position()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var teamId = managerStaff.User.TeamId!.Value;
        var first = await Host.CreateJobPositionAsync(teamId);
        var second = await Host.CreateJobPositionAsync(teamId);
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        var one = await manager.AddedAsync(first, CandidateApi.ValidInput("Fake One"));
        var two = await manager.AddedAsync(first, CandidateApi.ValidInput("Fake Two"));
        var otherOne = await manager.AddedAsync(second, CandidateApi.ValidInput("Fake Other"));
        var three = await manager.AddedAsync(first, CandidateApi.ValidInput("Fake One"));

        // Assert
        (one.Number, two.Number, three.Number).ShouldBe((1, 2, 3));
        otherOne.Number.ShouldBe(1);
        (await manager.CandidatesAsync(first)).Select(candidate => (candidate.Number, candidate.DisplayName))
            .ShouldBe([(1, "Fake One"), (2, "Fake Two"), (3, "Fake One")]);
    }

    [Fact]
    public async Task A_concurrent_add_to_the_same_position_takes_the_next_number()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);
        using var otherBrowser = await CreateBrowserAsync(managerStaff);
        Host.ConcurrentChange.BeforeNextSave<Candidate>(
            EntityState.Added,
            () => otherBrowser.AddedAsync(positionId, CandidateApi.ValidInput("Fake Faster")));

        // Act
        var added = await manager.AddedAsync(positionId, CandidateApi.ValidInput("Fake Slower"));

        // Assert
        added.Number.ShouldBe(2);
        (await manager.CandidatesAsync(positionId)).Select(candidate => (candidate.Number, candidate.DisplayName))
            .ShouldBe([(1, "Fake Faster"), (2, "Fake Slower")]);
    }

    [Fact]
    public async Task A_position_closed_during_an_add_takes_no_candidate()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);
        Host.ConcurrentChange.BeforeNextSave<Candidate>(EntityState.Added, () => Host.ClosePositionAsync(positionId));

        // Act
        using var response = await manager.AddAsync(positionId, CandidateApi.ValidInput());

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        (await manager.CandidatesAsync(positionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_closed_position_takes_no_new_candidates()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value, new JobPositionBuilder().Closed());
        using var manager = await CreateBrowserAsync(managerStaff);

        // Act
        using var response = await manager.AddAsync(positionId, CandidateApi.ValidInput());

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        (await manager.CandidatesAsync(positionId)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Manager_and_interviewer_edit_a_candidate(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var positionId = await Host.CreateJobPositionAsync(staff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var browser = await CreateBrowserAsync(staff);
        var before = await browser.CandidateAsync(candidateId);
        var input = new CandidateInput("Jane Q. Fakeperson", "jane.q@example.com", null, ["https://profiles.example.com/jane-q"]);

        // Act
        using var response = await browser.EditAsync(candidateId, input, before.Version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var after = await browser.CandidateAsync(candidateId);
        after.FullName.ShouldBe("Jane Q. Fakeperson");
        after.DisplayName.ShouldBe("Jane Q. Fakeperson");
        after.Email.ShouldBe("jane.q@example.com");
        after.Phone.ShouldBeNull();
        after.Links.ShouldBe(["https://profiles.example.com/jane-q"]);
        after.Number.ShouldBe(before.Number);
        after.CreatedAt.ShouldBe(before.CreatedAt);
        after.Version.ShouldNotBe(before.Version);
    }

    [Fact]
    public async Task A_stale_edit_is_a_conflict()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var manager = await CreateBrowserAsync(managerStaff);
        var staleVersion = (await manager.CandidateAsync(candidateId)).Version;
        using var first = await manager.EditAsync(candidateId, CandidateApi.ValidInput("Fake First Edit"), staleVersion);
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        using var response = await manager.EditAsync(candidateId, CandidateApi.ValidInput("Fake Second Edit"), staleVersion);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.ChangedMeanwhile");
        (await manager.CandidateAsync(candidateId)).FullName.ShouldBe("Fake First Edit");
    }

    [Fact]
    public async Task A_candidate_of_a_closed_position_cannot_be_edited()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake Before Closing"));
        await Host.ClosePositionAsync(positionId);
        using var manager = await CreateBrowserAsync(managerStaff);
        var version = (await manager.CandidateAsync(candidateId)).Version;

        // Act
        using var response = await manager.EditAsync(candidateId, CandidateApi.ValidInput("Fake After Closing"), version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        var candidate = await manager.CandidateAsync(candidateId);
        candidate.FullName.ShouldBeNull();
        candidate.DisplayName.ShouldBe("Candidate #1");
        candidate.PositionState.ShouldBe(JobPositionState.Closed);
    }

    [Fact]
    public async Task An_anonymised_candidate_shows_as_their_number_and_cannot_be_edited()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        await Host.AddCandidateAsync(positionId);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().Anonymised());
        using var manager = await CreateBrowserAsync(managerStaff);
        var before = await manager.CandidateAsync(candidateId);

        // Act
        using var response = await manager.EditAsync(candidateId, CandidateApi.ValidInput(), before.Version);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Candidate.Anonymised");
        before.DisplayName.ShouldBe("Candidate #2");
        before.IsAnonymised.ShouldBeTrue();
        before.FullName.ShouldBeNull();
        before.Email.ShouldBeNull();
        before.Links.ShouldBeEmpty();
        (await manager.CandidatesAsync(positionId)).Last().DisplayName.ShouldBe("Candidate #2");
    }

    [Fact]
    public async Task Another_team_candidates_are_invisible_and_cannot_be_changed()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var otherPositionId = await Host.CreateJobPositionAsync(otherTeam.Id);
        var otherCandidateId = await Host.AddCandidateAsync(otherPositionId);
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var listed = await manager.GetAsync(CandidateApi.CandidatesOf(otherPositionId));
        using var added = await manager.AddAsync(otherPositionId, CandidateApi.ValidInput());
        using var viewed = await manager.GetAsync(CandidateApi.Candidate(otherCandidateId));
        using var edited = await manager.EditAsync(otherCandidateId, CandidateApi.ValidInput(), version: 0);

        // Assert
        await listed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await added.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await viewed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        await edited.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        var otherManagerUser = await Host.CreateUserAsync(UserBuilder.Manager(otherTeam.Id));
        using var otherManager = await CreateBrowserAsync(new SignedInStaff(otherManagerUser, await Host.SignInAsync(otherManagerUser)));
        (await otherManager.CandidatesAsync(otherPositionId)).ShouldHaveSingleItem().Id.ShouldBe(otherCandidateId.Value);
    }

    [Fact]
    public async Task Admin_cannot_read_or_change_candidates()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var positionId = await Host.CreateJobPositionAsync(team.Id);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var listed = await admin.GetAsync(CandidateApi.CandidatesOf(positionId));
        using var added = await admin.AddAsync(positionId, CandidateApi.ValidInput());
        using var viewed = await admin.GetAsync(CandidateApi.Candidate(candidateId));
        using var edited = await admin.EditAsync(candidateId, CandidateApi.ValidInput(), version: 0);

        // Assert
        listed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        added.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        viewed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        edited.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_unknown_position_or_candidate_is_not_found()
    {
        // Arrange
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));
        var unknownPosition = JobPositionId.New();
        var unknownCandidate = CandidateId.New();

        // Act
        using var listed = await manager.GetAsync(CandidateApi.CandidatesOf(unknownPosition));
        using var added = await manager.AddAsync(unknownPosition, CandidateApi.ValidInput());
        using var viewed = await manager.GetAsync(CandidateApi.Candidate(unknownCandidate));
        using var edited = await manager.EditAsync(unknownCandidate, CandidateApi.ValidInput(), version: 1);

        // Assert
        await listed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await added.ShouldBeProblemAsync(HttpStatusCode.NotFound, "JobPosition.NotFound");
        await viewed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        await edited.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
    }

    private static readonly Dictionary<string, CandidateInput> _invalidInputs = new()
    {
        ["no full name"] = CandidateApi.ValidInput() with { FullName = null },
        ["a blank full name"] = CandidateApi.ValidInput() with { FullName = "   " },
        ["an overlong full name"] = CandidateApi.ValidInput() with { FullName = new string('x', CandidateDetails.FullNameMaxLength + 1) },
        ["a malformed email"] = CandidateApi.ValidInput() with { Email = "not-an-email" },
        ["an overlong email"] = CandidateApi.ValidInput() with { Email = $"{new string('x', CandidateDetails.EmailMaxLength)}@example.com" },
        ["a phone with letters"] = CandidateApi.ValidInput() with { Phone = "call me maybe" },
        ["four links"] = CandidateApi.ValidInput() with
        {
            Links = ["https://a.example.com", "https://b.example.com", "https://c.example.com", "https://d.example.com"],
        },
        ["a link without a scheme"] = CandidateApi.ValidInput() with { Links = ["profiles.example.com/no-scheme"] },
        ["an ftp link"] = CandidateApi.ValidInput() with { Links = ["ftp://files.example.com/cv"] },
        ["a javascript link"] = CandidateApi.ValidInput() with { Links = ["javascript:alert(1)"] },
        ["a blank link"] = CandidateApi.ValidInput() with { Links = [" "] },
        ["an overlong link"] = CandidateApi.ValidInput() with { Links = [$"https://example.com/{new string('x', CandidateDetails.LinkMaxLength)}"] },
    };

    public static TheoryData<string> InvalidInputs => new(_invalidInputs.Keys);

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public async Task Invalid_details_are_refused_on_add_and_edit(string invalid)
    {
        // Arrange
        var input = _invalidInputs[invalid];
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().WithFullName("Fake Unchanged"));
        using var manager = await CreateBrowserAsync(managerStaff);
        var version = (await manager.CandidateAsync(candidateId)).Version;

        // Act
        using var added = await manager.AddAsync(positionId, input);
        using var edited = await manager.EditAsync(candidateId, input, version);

        // Assert
        await added.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await edited.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        (await manager.CandidatesAsync(positionId)).ShouldHaveSingleItem().DisplayName.ShouldBe("Fake Unchanged");
    }

    [Fact]
    public async Task Three_links_are_allowed()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);
        List<string> links = ["https://a.example.com/fake", "http://b.example.com/fake", "https://c.example.com/fake?tab=1"];

        // Act
        var added = await manager.AddedAsync(positionId, CandidateApi.ValidInput() with { Links = links });

        // Assert
        (await manager.CandidateAsync(added.CandidateId)).Links.ShouldBe(links);
    }
}
