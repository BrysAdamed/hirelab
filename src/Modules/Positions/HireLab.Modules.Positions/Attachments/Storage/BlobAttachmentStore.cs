using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.Attachments.Storage;

internal sealed class BlobAttachmentStore(BlobServiceClient blobServiceClient) : IAttachmentStore
{
    private readonly BlobContainerClient _container = blobServiceClient.GetBlobContainerClient(PositionsModule.AttachmentsContainerName);

    public async Task UploadAsync(
        CandidateId candidateId,
        AttachmentId attachmentId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        };
        await Blob(candidateId, attachmentId).UploadAsync(content, options, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(CandidateId candidateId, AttachmentId attachmentId, CancellationToken cancellationToken)
    {
        try
        {
            var download = await Blob(candidateId, attachmentId).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (RequestFailedException exception) when (exception.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(CandidateId candidateId, AttachmentId attachmentId, CancellationToken cancellationToken) =>
        Blob(candidateId, attachmentId).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async IAsyncEnumerable<StoredAttachmentPage> ListPagesAsync(int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var page in _container.GetBlobsAsync(cancellationToken: cancellationToken).AsPages(pageSizeHint: pageSize))
        {
            var attachments = new List<StoredAttachment>(page.Values.Count);
            foreach (var blob in page.Values)
            {
                if (TryParse(blob.Name, out var candidateId, out var attachmentId) && blob.Properties.LastModified is { } lastModified)
                {
                    attachments.Add(new StoredAttachment(candidateId, attachmentId, lastModified));
                }
            }

            yield return new StoredAttachmentPage(attachments, page.Values.Count - attachments.Count);
        }
    }

    private BlobClient Blob(CandidateId candidateId, AttachmentId attachmentId) =>
        _container.GetBlobClient(Attachment.BlobNameFor(candidateId, attachmentId));

    private static bool TryParse(string blobName, out CandidateId candidateId, out AttachmentId attachmentId)
    {
        var parts = blobName.Split('/');
        candidateId = default;
        attachmentId = default;
        return parts.Length == 2
            && CandidateId.TryParse(parts[0], null, out candidateId)
            && AttachmentId.TryParse(parts[1], null, out attachmentId)
            && Attachment.BlobNameFor(candidateId, attachmentId) == blobName;
    }
}
