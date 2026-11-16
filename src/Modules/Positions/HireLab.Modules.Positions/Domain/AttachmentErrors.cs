using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Positions.Domain;

internal static class AttachmentErrors
{
    public static readonly Error NotFound = Error.NotFound("Attachment.NotFound", "The attachment was not found.");

    public static readonly Error Empty = Error.Validation("Attachment.Empty", "The file is empty.");

    public static readonly Error TooLarge = Error.Validation(
        "Attachment.TooLarge",
        $"The file is larger than {Attachment.MaxSizeInBytes / (1024 * 1024)} MB.");

    public static readonly Error UnsupportedType = Error.Validation(
        "Attachment.UnsupportedType",
        $"Only {string.Join(", ", AttachmentType.All.Select(type => type.Name))} files can be uploaded, and the content must match the extension.");

    public static readonly Error LimitReached = Error.Conflict(
        "Attachment.LimitReached",
        $"This candidate already has {Attachment.MaxPerCandidate} attachments. Delete one to upload another.");
}
