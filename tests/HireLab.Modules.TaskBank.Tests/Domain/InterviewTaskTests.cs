using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Tests.Tasks;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.TaskBank.Tests.Domain;

public sealed class InterviewTaskTests
{
    private static readonly UserId _manager = UserId.New();
    private static readonly DateTimeOffset _now = HireLabTestHost.StartTime;

    [Fact]
    public void A_team_task_starts_as_a_draft_without_an_active_version()
    {
        // Act
        var task = InterviewTask.CreateForTeam(TeamId.New(), TaskVisibility.Team, TestTasks.Valid(), _manager);

        // Assert
        task.IsSystem.ShouldBeFalse();
        task.Versions.Single().Number.ShouldBe(1);
        task.Draft.ShouldBe(task.Versions.Single());
        task.ActiveVersion.ShouldBeNull();
    }

    [Fact]
    public void The_newest_published_version_is_active_by_default()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New(), publishedVersions: 2, withDraft: true);

        // Act
        var active = task.ActiveVersion;

        // Assert
        active.ShouldNotBeNull().Number.ShouldBe(2);
        task.Draft.ShouldNotBeNull().Number.ShouldBe(3);
    }

    [Fact]
    public void A_switched_active_version_stays_active_after_newer_ones_are_published()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New(), publishedVersions: 2);
        var first = task.Versions.Single(version => version.Number == 1);

        // Act
        task.SwitchActiveVersion(first.Id).IsSuccess.ShouldBeTrue();
        task.StartDraft(TestTasks.Valid(), _manager).IsSuccess.ShouldBeTrue();
        task.PublishDraft(makeActive: false, _manager, _now).IsSuccess.ShouldBeTrue();

        // Assert
        task.ActiveVersion.ShouldBe(first);
    }

    [Fact]
    public void Publishing_without_making_it_active_keeps_the_newest_published_version_active()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New(), publishedVersions: 1, withDraft: true);

        // Act
        var published = task.PublishDraft(makeActive: false, _manager, _now);

        // Assert
        published.Value.Number.ShouldBe(2);
        task.ActiveVersion.ShouldNotBeNull().Number.ShouldBe(1);
    }

    [Fact]
    public void Publishing_and_making_it_active_overrides_a_switched_version()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New(), publishedVersions: 2);
        task.SwitchActiveVersion(task.Versions.Single(version => version.Number == 1).Id);
        task.StartDraft(TestTasks.Valid(), _manager);

        // Act
        task.PublishDraft(makeActive: true, _manager, _now);

        // Assert
        task.ActiveVersion.ShouldNotBeNull().Number.ShouldBe(3);
        task.ActiveVersionId.ShouldBeNull();
    }

    [Fact]
    public void Publishing_records_who_and_when()
    {
        // Arrange
        var task = InterviewTask.CreateForTeam(TeamId.New(), TaskVisibility.Global, TestTasks.Valid(), _manager);

        // Act
        var version = task.PublishDraft(makeActive: true, _manager, _now).Value;

        // Assert
        version.State.ShouldBe(TaskVersionState.Published);
        version.CreatedBy.ShouldBe(_manager);
        version.PublishedBy.ShouldBe(_manager);
        version.PublishedAt.ShouldBe(_now);
    }

    [Fact]
    public void A_task_has_at_most_one_draft()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New(), publishedVersions: 1, withDraft: true);

        // Act
        var result = task.StartDraft(TestTasks.Valid(), _manager);

        // Assert
        result.Error.ShouldBe(TaskErrors.DraftExists);
    }

    [Fact]
    public void Only_a_published_version_can_be_made_active()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New(), publishedVersions: 1, withDraft: true);

        // Act
        var draft = task.SwitchActiveVersion(task.Draft!.Id);
        var unknown = task.SwitchActiveVersion(Contracts.TaskVersionId.New());

        // Assert
        draft.Error.ShouldBe(TaskErrors.VersionNotPublished);
        unknown.Error.ShouldBe(TaskErrors.VersionNotFound);
    }

    [Fact]
    public void A_system_task_is_global_published_and_always_uses_its_newest_version()
    {
        // Act
        var task = TestTasks.System("fake-echo-task", new TaskVersionBuilder(), new TaskVersionBuilder());

        // Assert
        task.IsSystem.ShouldBeTrue();
        task.TeamId.ShouldBeNull();
        task.Visibility.ShouldBe(TaskVisibility.Global);
        task.Versions.ShouldAllBe(version => version.IsPublished && version.CreatedBy == null && version.PublishedBy == null);
        task.ActiveVersion.ShouldNotBeNull().Number.ShouldBe(2);
    }

    [Fact]
    public void A_system_task_is_read_only()
    {
        // Arrange
        var task = TestTasks.System("fake-echo-task", new TaskVersionBuilder(), new TaskVersionBuilder());

        // Act
        var results = new[]
        {
            task.StartDraft(TestTasks.Valid(), _manager),
            task.PublishDraft(makeActive: true, _manager, _now),
            task.SwitchActiveVersion(task.Versions.First().Id),
            task.Archive(_manager, _now),
        };

        // Assert
        results.ShouldAllBe(result => result.Error == TaskErrors.SystemTaskReadOnly);
    }

    [Fact]
    public void A_system_task_needs_a_kebab_case_slug()
    {
        // Act and assert
        Should.Throw<ArgumentException>(() => InterviewTask.CreateSystem("Fake Echo", TestTasks.Valid(), _now));
    }

    [Fact]
    public void A_team_task_gets_no_seed_versions()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New());

        // Act and assert
        Should.Throw<InvalidOperationException>(() => task.AddSystemVersion(TestTasks.Valid(), _now));
    }

    [Fact]
    public void An_archived_task_records_who_and_when_and_can_no_longer_change()
    {
        // Arrange
        var task = TestTasks.Team(TeamId.New());

        // Act
        task.Archive(_manager, _now).IsSuccess.ShouldBeTrue();

        // Assert
        task.IsArchived.ShouldBeTrue();
        task.ArchivedBy.ShouldBe(_manager);
        task.ArchivedAt.ShouldBe(_now);
        task.StartDraft(TestTasks.Valid(), _manager).Error.ShouldBe(TaskErrors.Archived);
    }
}
