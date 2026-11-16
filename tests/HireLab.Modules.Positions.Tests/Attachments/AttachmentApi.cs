using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HireLab.Modules.Positions.Contracts;

namespace HireLab.Modules.Positions.Tests.Attachments;

internal static class AttachmentApi
{
    public static Uri AttachmentsOf(CandidateId candidateId) => new($"/api/staff/candidates/{candidateId}/attachments", UriKind.Relative);

    public static Uri Attachment(CandidateId candidateId, Guid attachmentId) =>
        new($"/api/staff/candidates/{candidateId}/attachments/{attachmentId}", UriKind.Relative);

    public static Task<HttpResponseMessage> UploadAsync(
        this BrowserSession browser,
        CandidateId candidateId,
        string fileName,
        byte[] content,
        string declaredContentType = "application/octet-stream")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(declaredContentType);
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return browser.SendAsync(new HttpRequestMessage(HttpMethod.Post, AttachmentsOf(candidateId)) { Content = form });
    }

    public static async Task<UploadedAttachmentView> UploadedAsync(this BrowserSession browser, CandidateId candidateId, string fileName, byte[] content)
    {
        using var response = await browser.UploadAsync(candidateId, fileName, content);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var uploaded = (await response.Content.ReadFromJsonAsync<UploadedAttachmentView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        response.Headers.Location.ShouldBe(Attachment(candidateId, uploaded.Id));
        return uploaded;
    }

    public static async Task<List<AttachmentView>> AttachmentsAsync(this BrowserSession browser, CandidateId candidateId)
    {
        using var response = await browser.GetAsync(AttachmentsOf(candidateId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<AttachmentView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> DownloadAsync(this BrowserSession browser, CandidateId candidateId, Guid attachmentId) =>
        browser.GetAsync(Attachment(candidateId, attachmentId));

    public static Task<HttpResponseMessage> DeleteAsync(this BrowserSession browser, CandidateId candidateId, Guid attachmentId) =>
        browser.DeleteAsync(Attachment(candidateId, attachmentId));
}

internal sealed record UploadedAttachmentView(Guid Id, string FileName, string ContentType, long Size);

internal sealed record AttachmentView(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset UploadedAt,
    Guid UploadedById,
    string? UploadedByName);
