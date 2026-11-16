using Azure.Storage.Blobs.Specialized;
using HireLab.Modules.Positions.Attachments.OrphanSweep;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Tests.Candidates;
using HireLab.Modules.Positions.Tests.JobPositions;
using HireLab.SharedKernel.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HireLab.Modules.Positions.Tests.Attachments;

public sealed class OrphanAttachmentSweepTests(HireLabTestHost host) : IntegrationTest(host)
{
    private static readonly TimeSpan _gracePeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan _waitLimit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task An_orphan_older_than_the_grace_period_is_deleted()
    {
        // Arrange
        var orphan = await StoreOrphanAsync();

        // Act
        var result = await SweepAtAsync(orphan.LastModified + _gracePeriod + TimeSpan.FromSeconds(1));

        // Assert
        result.ShouldBe(new OrphanAttachmentSweepResult(Checked: 1, Deleted: 1, Failed: 0, Unrecognised: 0));
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task An_orphan_within_the_grace_period_is_kept_as_it_may_be_an_upload_in_flight()
    {
        // Arrange
        var orphan = await StoreOrphanAsync();

        // Act
        var result = await SweepAtAsync(orphan.LastModified + _gracePeriod - TimeSpan.FromSeconds(1));

        // Assert
        result.ShouldBe(new OrphanAttachmentSweepResult(Checked: 1, Deleted: 0, Failed: 0, Unrecognised: 0));
        (await BlobNamesAsync()).ShouldBe([orphan.Name]);
    }

    [Fact]
    public async Task Blobs_with_an_attachment_row_of_any_team_are_kept()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var otherManager = await SignInAsAsync(StaffRole.Manager);
        var blobs = new List<string>();
        foreach (var staff in new[] { manager, otherManager })
        {
            var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(staff.User.TeamId!.Value));
            using var browser = await CreateBrowserAsync(staff);
            var uploaded = await browser.UploadedAsync(candidateId, "Fake CV.pdf", FakeFiles.Pdf);
            blobs.Add($"{candidateId}/{uploaded.Id}");
        }

        // Act
        var result = await SweepAtAsync(DateTimeOffset.UtcNow + TimeSpan.FromDays(30));

        // Assert
        result.ShouldBe(new OrphanAttachmentSweepResult(Checked: 2, Deleted: 0, Failed: 0, Unrecognised: 0));
        (await BlobNamesAsync()).ShouldBe(blobs, ignoreOrder: true);
    }

    [Fact]
    public async Task A_blob_named_after_an_attachment_under_another_candidate_is_an_orphan()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(staff.User.TeamId!.Value));
        using var browser = await CreateBrowserAsync(staff);
        var uploaded = await browser.UploadedAsync(candidateId, "Fake CV.pdf", FakeFiles.Pdf);
        var misplaced = await StoreBlobAsync($"{CandidateId.New()}/{uploaded.Id}");

        // Act
        var result = await SweepAtAsync(misplaced.LastModified + _gracePeriod + TimeSpan.FromSeconds(1));

        // Assert
        result.ShouldBe(new OrphanAttachmentSweepResult(Checked: 2, Deleted: 1, Failed: 0, Unrecognised: 0));
        (await BlobNamesAsync()).ShouldBe([$"{candidateId}/{uploaded.Id}"]);
    }

    [Fact]
    public async Task Blobs_whose_name_is_not_an_attachment_blob_name_are_kept_and_counted()
    {
        // Arrange
        string[] unrecognised =
        [
            "not-an-attachment.txt",
            $"{Guid.CreateVersion7()}/{Guid.CreateVersion7()}/extra",
            $"{Guid.CreateVersion7():N}/{Guid.CreateVersion7():N}",
            $"{Guid.CreateVersion7()}/not-a-guid",
        ];
        foreach (var name in unrecognised)
        {
            await StoreBlobAsync(name);
        }

        // Act
        var result = await SweepAtAsync(DateTimeOffset.UtcNow + TimeSpan.FromDays(30));

        // Assert
        result.ShouldBe(new OrphanAttachmentSweepResult(Checked: 0, Deleted: 0, Failed: 0, Unrecognised: unrecognised.Length));
        (await BlobNamesAsync()).ShouldBe(unrecognised, ignoreOrder: true);
    }

    [Fact]
    public async Task A_failed_deletion_is_counted_and_the_sweep_goes_on_across_pages()
    {
        // Arrange
        var orphans = new List<StoredBlob>();
        for (var i = 0; i < 3; i++)
        {
            orphans.Add(await StoreOrphanAsync());
        }

        var leased = orphans[1];
        var lease = Host.AttachmentBlobs.GetBlobClient(leased.Name).GetBlobLeaseClient();
        await lease.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: CancellationToken);

        try
        {
            // Act
            var result = await SweepAtAsync(orphans.Max(orphan => orphan.LastModified) + _gracePeriod + TimeSpan.FromSeconds(1), pageSize: 1);

            // Assert
            result.ShouldBe(new OrphanAttachmentSweepResult(Checked: 3, Deleted: 2, Failed: 1, Unrecognised: 0));
            (await BlobNamesAsync()).ShouldBe([leased.Name]);
        }
        finally
        {
            await lease.BreakAsync(TimeSpan.Zero, cancellationToken: CancellationToken);
        }
    }

    [Fact]
    public async Task The_hosted_service_sweeps_repeatedly_and_survives_a_failed_sweep()
    {
        // Arrange
        var clock = Host.Time;
        var orphansAreOld = WholeSeconds(DateTimeOffset.UtcNow + _gracePeriod + TimeSpan.FromHours(1));
        if (clock.GetUtcNow() < orphansAreOld)
        {
            clock.SetUtcNow(orphansAreOld);
        }

        var logger = new FakeLogger<OrphanAttachmentSweep>();
        var options = Options.Create(new OrphanAttachmentSweepOptions { Interval = OrphanAttachmentSweepOptions.MinInterval });
        using var sweep = ActivatorUtilities.CreateInstance<OrphanAttachmentSweep>(Host.Services, logger, options);
        var first = await StoreOrphanAsync();
        await sweep.StartAsync(CancellationToken);

        try
        {
            // Act
            await WaitUntilAsync(async () => !await BlobExistsAsync(first.Name), advancing: clock);

            await Host.AttachmentBlobs.DeleteAsync(cancellationToken: CancellationToken);
            await WaitUntilAsync(() => Task.FromResult(logger.Collector.GetSnapshot().Any(record => record.Level == LogLevel.Error)), advancing: clock);

            await Host.AttachmentBlobs.CreateIfNotExistsAsync(cancellationToken: CancellationToken);
            var second = await StoreOrphanAsync();

            // Assert
            await WaitUntilAsync(async () => !await BlobExistsAsync(second.Name), advancing: clock);
        }
        finally
        {
            await sweep.StopAsync(CancellationToken);
            await Host.AttachmentBlobs.CreateIfNotExistsAsync(cancellationToken: CancellationToken);
        }
    }
    private async Task<OrphanAttachmentSweepResult> SweepAtAsync(DateTimeOffset now, int pageSize = 500)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var sweeper = ActivatorUtilities.CreateInstance<OrphanAttachmentSweeper>(scope.ServiceProvider, new FakeTimeProvider(now), SweepOptions(pageSize));
        return await sweeper.SweepAsync(CancellationToken);
    }

    private static IOptions<OrphanAttachmentSweepOptions> SweepOptions(int pageSize) =>
        Options.Create(new OrphanAttachmentSweepOptions { Enabled = true, Interval = _interval, GracePeriod = _gracePeriod, PageSize = pageSize });

    private Task<StoredBlob> StoreOrphanAsync() => StoreBlobAsync(Attachment.BlobNameFor(CandidateId.New(), AttachmentId.New()));

    private async Task<StoredBlob> StoreBlobAsync(string name)
    {
        var uploaded = await Host.AttachmentBlobs.UploadBlobAsync(name, BinaryData.FromBytes(FakeFiles.Pdf), CancellationToken);
        return new StoredBlob(name, uploaded.Value.LastModified);
    }

    private async Task<bool> BlobExistsAsync(string name) =>
        (await Host.AttachmentBlobs.GetBlobClient(name).ExistsAsync(CancellationToken)).Value;

    private async Task<List<string>> BlobNamesAsync()
    {
        var names = new List<string>();
        await foreach (var blob in Host.AttachmentBlobs.GetBlobsAsync(cancellationToken: CancellationToken))
        {
            names.Add(blob.Name);
        }

        return names;
    }

    private static DateTimeOffset WholeSeconds(DateTimeOffset time) =>
        new(time.Ticks - (time.Ticks % TimeSpan.TicksPerSecond), time.Offset);

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, FakeTimeProvider advancing)
    {
        var deadline = DateTimeOffset.UtcNow + _waitLimit;
        while (!await condition())
        {
            DateTimeOffset.UtcNow.ShouldBeLessThan(deadline, "The condition wasn't met in time");
            advancing.Advance(TimeSpan.FromSeconds(10));
            await Task.Delay(TimeSpan.FromMilliseconds(50), CancellationToken);
        }
    }

    private sealed record StoredBlob(string Name, DateTimeOffset LastModified);
}
