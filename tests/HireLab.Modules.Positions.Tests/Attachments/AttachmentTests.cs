using System.Net;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Tests.Candidates;
using HireLab.Modules.Positions.Tests.JobPositions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.Attachments;

public sealed class AttachmentTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const int MaxSize = 10 * 1024 * 1024;

    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Manager_and_interviewer_upload_list_download_and_delete_an_attachment(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        var uploaded = await browser.UploadedAsync(candidateId, "Fake CV.pdf", FakeFiles.Pdf);
        var listed = await browser.AttachmentsAsync(candidateId);
        using var download = await browser.DownloadAsync(candidateId, uploaded.Id);
        var downloaded = await download.Content.ReadAsByteArrayAsync(CancellationToken);
        using var deleted = await browser.DeleteAsync(candidateId, uploaded.Id);

        // Assert
        uploaded.ShouldBe(new UploadedAttachmentView(uploaded.Id, "Fake CV.pdf", "application/pdf", FakeFiles.Pdf.Length));
        listed.ShouldHaveSingleItem().ShouldBe(new AttachmentView(
            uploaded.Id,
            "Fake CV.pdf",
            "application/pdf",
            FakeFiles.Pdf.Length,
            Host.Time.GetUtcNow(),
            staff.User.Id.Value,
            staff.User.DisplayName));

        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        downloaded.ShouldBe(FakeFiles.Pdf);
        download.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        download.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        download.Content.Headers.ContentDisposition.FileNameStar.ShouldBe("Fake CV.pdf");
        download.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        download.Headers.CacheControl!.NoStore.ShouldBeTrue();

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await browser.AttachmentsAsync(candidateId)).ShouldBeEmpty();
        using var downloadAfterDelete = await browser.DownloadAsync(candidateId, uploaded.Id);
        await downloadAfterDelete.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Attachment.NotFound");
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_blob_is_named_by_candidate_and_attachment_and_holds_the_detected_content_type()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.UploadAsync(candidateId, "photo.png", FakeFiles.Png, declaredContentType: "text/html");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var attachment = (await browser.AttachmentsAsync(candidateId)).ShouldHaveSingleItem();
        attachment.ContentType.ShouldBe("image/png");
        (await BlobNamesAsync()).ShouldBe([$"{candidateId}/{attachment.Id}"]);
        var properties = await Host.AttachmentBlobs.GetBlobClient($"{candidateId}/{attachment.Id}").GetPropertiesAsync(cancellationToken: CancellationToken);
        properties.Value.ContentType.ShouldBe("image/png");
    }

    [Fact]
    public async Task The_original_file_name_without_directories_is_kept_and_encoded_for_the_download()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        var uploaded = await browser.UploadedAsync(candidateId, "C:\\fake\\Życiorys Zoë ✓.pdf", FakeFiles.Pdf);
        using var download = await browser.DownloadAsync(candidateId, uploaded.Id);

        // Assert
        uploaded.FileName.ShouldBe("Życiorys Zoë ✓.pdf");
        var disposition = download.Content.Headers.ContentDisposition.ShouldNotBeNull();
        disposition.DispositionType.ShouldBe("attachment");
        disposition.FileNameStar.ShouldBe("Życiorys Zoë ✓.pdf");
        download.Content.Headers.GetValues("Content-Disposition").Single().ShouldContain("filename*=UTF-8''");
    }

    [Fact]
    public async Task A_file_of_10_MB_is_accepted_and_a_larger_or_empty_one_is_refused()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var atLimit = await browser.UploadAsync(candidateId, "at-limit.pdf", FakeFiles.PdfOfSize(MaxSize));
        using var overLimit = await browser.UploadAsync(candidateId, "over-limit.pdf", FakeFiles.PdfOfSize(MaxSize + 1));
        using var empty = await browser.UploadAsync(candidateId, "empty.pdf", []);

        // Assert
        atLimit.StatusCode.ShouldBe(HttpStatusCode.Created);
        await overLimit.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Attachment.TooLarge");
        await empty.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Attachment.Empty");
        (await browser.AttachmentsAsync(candidateId)).ShouldHaveSingleItem().Size.ShouldBe(MaxSize);
        (await BlobNamesAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public void The_upload_endpoint_limits_the_request_body_so_the_server_refuses_a_larger_one_before_reading_it()
    {
        // Arrange
        var endpoints = Host.Services.GetRequiredService<EndpointDataSource>().Endpoints;

        // Act
        var upload = endpoints.Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "UploadAttachment");

        // Assert
        upload.Metadata.GetMetadata<IRequestSizeLimitMetadata>().ShouldNotBeNull().MaxRequestBodySize.ShouldBe(MaxSize + (64 * 1024));
    }

    [Fact]
    public async Task A_request_body_far_over_the_limit_stores_nothing()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.UploadAsync(candidateId, "huge.pdf", FakeFiles.PdfOfSize(MaxSize + (1024 * 1024)));

        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse();
        (await browser.AttachmentsAsync(candidateId)).ShouldBeEmpty();
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    public static TheoryData<string, byte[]> RefusedFiles => new()
    {
        { "program.exe", FakeFiles.Executable },
        { "renamed-program.pdf", FakeFiles.Executable },
        { "picture-renamed.pdf", FakeFiles.Png },
        { "binary.txt", FakeFiles.Executable },
    };

    [Theory]
    [MemberData(nameof(RefusedFiles))]
    public async Task A_file_of_another_type_or_with_content_that_does_not_match_its_extension_is_refused(string fileName, byte[] content)
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.UploadAsync(candidateId, fileName, content, declaredContentType: "application/pdf");

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Attachment.UnsupportedType");
        (await browser.AttachmentsAsync(candidateId)).ShouldBeEmpty();
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_candidate_has_at_most_10_attachments_and_a_deleted_one_frees_a_place()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        var uploaded = new List<UploadedAttachmentView>();
        for (var number = 1; number <= 10; number++)
        {
            uploaded.Add(await browser.UploadedAsync(candidateId, $"file-{number}.txt", FakeFiles.Text($"Fake file {number}")));
        }

        // Act
        using var eleventh = await browser.UploadAsync(candidateId, "file-11.txt", FakeFiles.Text("One too many"));
        using var deleted = await browser.DeleteAsync(candidateId, uploaded[3].Id);
        using var replacement = await browser.UploadAsync(candidateId, "replacement.md", FakeFiles.Text("# Fake replacement"));

        // Assert
        await eleventh.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Attachment.LimitReached");
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        replacement.StatusCode.ShouldBe(HttpStatusCode.Created);
        var listed = await browser.AttachmentsAsync(candidateId);
        listed.Count.ShouldBe(10);
        listed.ShouldContain(attachment => attachment.FileName == "replacement.md");
        listed.ShouldNotContain(attachment => attachment.FileName == "file-11.txt");
        (await BlobNamesAsync()).Count.ShouldBe(10);
    }

    [Fact]
    public async Task Concurrent_uploads_take_different_places()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        using var otherBrowser = await CreateBrowserAsync(staff);
        Host.ConcurrentChange.BeforeNextSave<Attachment>(
            EntityState.Added,
            () => otherBrowser.UploadedAsync(candidateId, "faster.pdf", FakeFiles.Pdf));

        // Act
        using var response = await browser.UploadAsync(candidateId, "slower.pdf", FakeFiles.Pdf);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await browser.AttachmentsAsync(candidateId)).Select(attachment => attachment.FileName).Order().ShouldBe(["faster.pdf", "slower.pdf"]);
        (await BlobNamesAsync()).Count.ShouldBe(2);
    }

    [Fact]
    public async Task An_upload_that_loses_the_last_place_to_a_concurrent_one_is_refused_and_leaves_no_blob()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        using var otherBrowser = await CreateBrowserAsync(staff);
        for (var number = 1; number <= 9; number++)
        {
            await browser.UploadedAsync(candidateId, $"file-{number}.txt", FakeFiles.Text($"Fake file {number}"));
        }

        Host.ConcurrentChange.BeforeNextSave<Attachment>(
            EntityState.Added,
            () => otherBrowser.UploadedAsync(candidateId, "faster.pdf", FakeFiles.Pdf));

        // Act
        using var response = await browser.UploadAsync(candidateId, "slower.pdf", FakeFiles.Pdf);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Attachment.LimitReached");
        var listed = await browser.AttachmentsAsync(candidateId);
        listed.Count.ShouldBe(10);
        listed.ShouldNotContain(attachment => attachment.FileName == "slower.pdf");
        (await BlobNamesAsync()).Count.ShouldBe(10);
    }

    [Fact]
    public async Task A_candidate_of_a_closed_position_takes_no_uploads()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(staff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId);
        using var browser = await CreateBrowserAsync(staff);
        await Host.ClosePositionAsync(positionId);

        // Act
        using var upload = await browser.UploadAsync(candidateId, "late.pdf", FakeFiles.Pdf);

        // Assert
        await upload.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.Closed");
        (await browser.AttachmentsAsync(candidateId)).ShouldBeEmpty();
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task An_anonymised_candidate_takes_no_uploads()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var positionId = await Host.CreateJobPositionAsync(staff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId, new CandidateBuilder().Anonymised());
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.UploadAsync(candidateId, "cv.pdf", FakeFiles.Pdf);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Candidate.Anonymised");
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Another_team_attachments_are_invisible_and_cannot_be_changed()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var otherManagerUser = await Host.CreateUserAsync(UserBuilder.Manager(otherTeam.Id));
        var otherCandidateId = await AddCandidateAsync(otherTeam.Id);
        using var otherManager = await CreateBrowserAsync(new SignedInStaff(otherManagerUser, await Host.SignInAsync(otherManagerUser)));
        var otherAttachment = await otherManager.UploadedAsync(otherCandidateId, "cv.pdf", FakeFiles.Pdf);
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Interviewer));

        // Act
        using var listed = await manager.GetAsync(AttachmentApi.AttachmentsOf(otherCandidateId));
        using var uploaded = await manager.UploadAsync(otherCandidateId, "intruder.pdf", FakeFiles.Pdf);
        using var downloaded = await manager.DownloadAsync(otherCandidateId, otherAttachment.Id);
        using var deleted = await manager.DeleteAsync(otherCandidateId, otherAttachment.Id);

        // Assert
        await listed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        await uploaded.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        await downloaded.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Attachment.NotFound");
        await deleted.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Attachment.NotFound");
        (await otherManager.AttachmentsAsync(otherCandidateId)).ShouldHaveSingleItem().Id.ShouldBe(otherAttachment.Id);
        (await BlobNamesAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Admin_cannot_read_or_change_attachments()
    {
        // Arrange
        var managerStaff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(managerStaff.User.TeamId!.Value);
        using var manager = await CreateBrowserAsync(managerStaff);
        var attachment = await manager.UploadedAsync(candidateId, "cv.pdf", FakeFiles.Pdf);
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var listed = await admin.GetAsync(AttachmentApi.AttachmentsOf(candidateId));
        using var uploaded = await admin.UploadAsync(candidateId, "admin.pdf", FakeFiles.Pdf);
        using var downloaded = await admin.DownloadAsync(candidateId, attachment.Id);
        using var deleted = await admin.DeleteAsync(candidateId, attachment.Id);

        // Assert
        listed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        uploaded.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        downloaded.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        deleted.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await manager.AttachmentsAsync(candidateId)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_attachment_is_found_only_under_its_own_candidate()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var positionId = await Host.CreateJobPositionAsync(staff.User.TeamId!.Value);
        var candidateId = await Host.AddCandidateAsync(positionId);
        var otherCandidateId = await Host.AddCandidateAsync(positionId);
        using var browser = await CreateBrowserAsync(staff);
        var attachment = await browser.UploadedAsync(candidateId, "cv.pdf", FakeFiles.Pdf);

        // Act
        using var downloaded = await browser.DownloadAsync(otherCandidateId, attachment.Id);
        using var deleted = await browser.DeleteAsync(otherCandidateId, attachment.Id);
        using var unknownCandidate = await browser.GetAsync(AttachmentApi.AttachmentsOf(CandidateId.New()));

        // Assert
        await downloaded.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Attachment.NotFound");
        await deleted.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Attachment.NotFound");
        await unknownCandidate.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        (await browser.AttachmentsAsync(candidateId)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_upload_without_the_antiforgery_header_is_refused()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await AddCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        browser.SendsAntiforgeryHeader = false;

        // Act
        using var response = await browser.UploadAsync(candidateId, "cv.pdf", FakeFiles.Pdf);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Antiforgery.InvalidToken");
        (await BlobNamesAsync()).ShouldBeEmpty();
    }

    private async Task<CandidateId> AddCandidateAsync(TeamId teamId) =>
        await Host.AddCandidateAsync(await Host.CreateJobPositionAsync(teamId));

    private async Task<List<string>> BlobNamesAsync()
    {
        var names = new List<string>();
        await foreach (var blob in Host.AttachmentBlobs.GetBlobsAsync(cancellationToken: CancellationToken))
        {
            names.Add(blob.Name);
        }

        return names;
    }
}
