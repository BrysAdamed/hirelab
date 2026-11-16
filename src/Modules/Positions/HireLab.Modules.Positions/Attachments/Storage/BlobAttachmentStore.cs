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

    public async IAsyncEnumerable<StoredAttachment> ListAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in _container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (TryParse(blob.Name, out var candidateId, out var attachmentId) && blob.Properties.LastModified is { } lastModified)
            {
                yield return new StoredAttachment(candidateId, attachmentId, lastModified);
            }
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
            && AttachmentId.TryParse(parts[1], null, out attachmentId);
    }
}
