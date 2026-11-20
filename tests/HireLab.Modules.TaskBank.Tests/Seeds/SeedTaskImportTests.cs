using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.Modules.TaskBank.Seeds;
using HireLab.Modules.TaskBank.Tests.Tasks;
using HireLab.Tasks;
using HireLab.TaskTypes.CodeReview;
using HireLab.TaskTypes.OpenQuestion;
using HireLab.Testing.TaskTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.TaskBank.Tests.Seeds;

public sealed class SeedTaskImportTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task The_first_import_creates_published_system_tasks()
    {
        // Arrange
        var packages = new FakeSeedPackages(
            new TestEchoSeedPackage("fake-echo-alpha", 1),
            new TestEchoSeedPackage("fake-echo-alpha", 2).WithPhrase("Second fake phrase"),
            new TestEchoSeedPackage("fake-echo-beta"));

        // Act
        await SeedImportJob.RunAsync(Host, packages);

        // Assert
        var tasks = await SystemTasksAsync();
        tasks.Select(task => task.Slug).ShouldBe(["fake-echo-alpha", "fake-echo-beta"]);
        var alpha = tasks[0];
        alpha.IsSystem.ShouldBeTrue();
        alpha.TeamId.ShouldBeNull();
        alpha.Visibility.ShouldBe(TaskVisibility.Global);
        alpha.TaskTypeKey.ShouldBe(TestEchoTaskType.TaskTypeKey);
        alpha.Versions.Select(version => version.Number).Order().ShouldBe([1, 2]);
        alpha.Versions.ShouldAllBe(version => version.IsPublished && version.PublishedAt == Host.Time.GetUtcNow() && version.PublishedBy == null);
        alpha.ActiveVersion!.Number.ShouldBe(2);
        alpha.ActiveVersion.CandidateContent.GetProperty("phrase").GetString().ShouldBe("Second fake phrase");
        alpha.ActiveVersion.ReferenceAnswer.ShouldBe(TestEchoSeedPackage.ReferenceAnswer);
        alpha.ActiveVersion.ContentHash.ShouldBe(HashOf(packages, "fake-echo-alpha/v2"));
    }

    [Fact]
    public async Task Importing_again_changes_nothing()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task"));
        await SeedImportJob.RunAsync(Host, packages);
        var imported = (await SystemTasksAsync()).ShouldHaveSingleItem();
        Host.Time.Advance(TimeSpan.FromDays(1));

        // Act
        await SeedImportJob.RunAsync(Host, packages);

        // Assert
        var task = (await SystemTasksAsync()).ShouldHaveSingleItem();
        task.Id.ShouldBe(imported.Id);
        var version = task.Versions.ShouldHaveSingleItem();
        version.Id.ShouldBe(imported.Versions.Single().Id);
        version.PublishedAt.ShouldBe(imported.Versions.Single().PublishedAt);
    }

    [Fact]
    public async Task A_new_version_is_added_to_the_imported_task()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task"));
        await SeedImportJob.RunAsync(Host, packages);
        var firstVersionId = (await SystemTasksAsync()).Single().Versions.Single().Id;
        packages.Add(new TestEchoSeedPackage("fake-echo-task", 2).WithPhrase("A newer fake phrase"));

        // Act
        await SeedImportJob.RunAsync(Host, packages);

        // Assert
        var task = (await SystemTasksAsync()).ShouldHaveSingleItem();
        task.Versions.Select(version => version.Number).Order().ShouldBe([1, 2]);
        task.Versions.Single(version => version.Number == 1).Id.ShouldBe(firstVersionId);
        task.ActiveVersion!.CandidateContent.GetProperty("phrase").GetString().ShouldBe("A newer fake phrase");
    }

    [Fact]
    public async Task A_rewritten_package_with_the_same_content_is_not_a_change()
    {
        // Arrange
        await SeedImportJob.RunAsync(Host, new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")));
        var rewritten = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")
            .With("referenceAnswer", TestEchoSeedPackage.ReferenceAnswer)
            .WithoutFile(TestEchoSeedPackage.ReferenceAnswerFile));

        // Act
        await SeedImportJob.RunAsync(Host, rewritten);

        // Assert
        (await SystemTasksAsync()).Single().Versions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_changed_imported_version_fails_the_import_and_imports_nothing()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task"));
        await SeedImportJob.RunAsync(Host, packages);
        packages
            .Replace(new TestEchoSeedPackage("fake-echo-task").WithPhrase("An edited fake phrase"))
            .Add(new TestEchoSeedPackage("fake-echo-other"));

        // Act
        var exception = await Should.ThrowAsync<SeedPackageException>(() => SeedImportJob.RunAsync(Host, packages));

        // Assert
        exception.Message.ShouldStartWith("Seed package fake-echo-task/v1 differs from the imported version");
        var task = (await SystemTasksAsync()).ShouldHaveSingleItem();
        task.Slug.ShouldBe("fake-echo-task");
        task.Versions.Single().CandidateContent.GetProperty("phrase").GetString().ShouldBe("Hello, fake world");
    }

    [Fact]
    public async Task An_unknown_task_type_fails_the_import()
    {
        // Arrange
        var packages = new FakeSeedPackages(
            new TestEchoSeedPackage("fake-echo-task"),
            new TestEchoSeedPackage("fake-unknown-task").With("taskType", "fake-unknown-type"));

        // Act
        var exception = await Should.ThrowAsync<SeedPackageException>(() => SeedImportJob.RunAsync(Host, packages));

        // Assert
        exception.Message.ShouldContain("'fake-unknown-type', which isn't registered");
        (await SystemTasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Invalid_content_fails_the_import()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").WithPhrase(" "));

        // Act
        var exception = await Should.ThrowAsync<SeedPackageException>(() => SeedImportJob.RunAsync(Host, packages));

        // Assert
        exception.Message.ShouldContain("candidateContent.phrase: Enter a phrase.");
        (await SystemTasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_draft_package_fails_the_import()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").With("draft", true));

        // Act
        var exception = await Should.ThrowAsync<SeedPackageException>(() => SeedImportJob.RunAsync(Host, packages));

        // Assert
        exception.Message.ShouldContain("Seed packages are always published");
        (await SystemTasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_version_gap_fails_the_import()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task", 1), new TestEchoSeedPackage("fake-echo-task", 3));

        // Act
        var exception = await Should.ThrowAsync<SeedPackageException>(() => SeedImportJob.RunAsync(Host, packages));

        // Assert
        exception.Message.ShouldContain("Number them from v1 without gaps");
        (await SystemTasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Nothing_is_read_or_imported_when_the_import_is_off()
    {
        // Arrange
        var packages = new FakeSeedPackages(
            new TestEchoSeedPackage("fake-echo-task"),
            new TestEchoSeedPackage("fake-draft-task").With("draft", true));

        // Act
        await SeedImportJob.RunAsync(Host, packages, importSeedTasks: false);

        // Assert
        (await SystemTasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_shipped_seed_packages_import_as_system_tasks()
    {
        // Arrange
        var packages = new EmbeddedSeedPackageSource(SeedTaskPackages.Assembly, SeedTaskPackages.ResourcePrefix);

        // Act
        await SeedImportJob.RunAsync(Host, packages);

        // Assert
        var tasks = await SystemTasksAsync();
        var openQuestion = tasks.Single(task => task.Slug == "explain-dependency-injection");
        openQuestion.TaskTypeKey.ShouldBe(OpenQuestionTaskType.TaskTypeKey);
        openQuestion.ActiveVersion!.CandidateContent.GetProperty("prompt").GetString().ShouldStartWith("## Dependency injection");
        openQuestion.ActiveVersion.StaffContent.EnumerateObject().ShouldBeEmpty();

        var codeReview = tasks.Single(task => task.Slug == "donation-request-cqrs-review");
        codeReview.TaskTypeKey.ShouldBe(CodeReviewTaskType.TaskTypeKey);
        var candidateContent = codeReview.ActiveVersion!.CandidateContent;
        candidateContent.GetProperty("files").GetArrayLength().ShouldBe(7);
        candidateContent.GetProperty("description").GetString()!.ShouldContain("If anything is unclear, ask.");
        codeReview.ActiveVersion.StaffContent.GetProperty("expectedFindings").EnumerateArray()
            .ShouldContain(finding => finding.GetProperty("id").GetString() == "identity-from-request-body");
    }

    private static string HashOf(FakeSeedPackages packages, string name)
    {
        var package = packages.Read().Single(package => package.Name == name);
        return TestTasks.Validator.Validate(package.TaskTypeKey, package.Details).Value.ContentHash;
    }

    private async Task<List<InterviewTask>> SystemTasksAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        return await dbContext.Tasks
            .IgnoreQueryFilters()
            .Include(task => task.Versions)
            .Where(task => task.IsSystem)
            .OrderBy(task => task.Slug)
            .ToListAsync(CancellationToken);
    }
}
