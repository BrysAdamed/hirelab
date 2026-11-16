namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// A file type an attachment may have (ADR 0005). <see cref="ContentType"/> is what downloads are served with, never the
/// content type the uploader declared.
/// </summary>
internal sealed record AttachmentType(string Name, string ContentType, IReadOnlyList<string> Extensions)
{
    public static readonly AttachmentType Pdf = new("PDF", "application/pdf", [".pdf"]);

    public static readonly AttachmentType Docx = new(
        "DOCX",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".docx"]);

    public static readonly AttachmentType Png = new("PNG", "image/png", [".png"]);

    public static readonly AttachmentType Jpeg = new("JPG", "image/jpeg", [".jpg", ".jpeg"]);

    public static readonly AttachmentType Text = new("TXT", "text/plain; charset=utf-8", [".txt"]);

    public static readonly AttachmentType Markdown = new("MD", "text/markdown; charset=utf-8", [".md"]);

    public static IReadOnlyList<AttachmentType> All { get; } = [Pdf, Docx, Png, Jpeg, Text, Markdown];

    /// <summary>The type whose extensions include the extension of <paramref name="fileName"/>, ignoring case.</summary>
    public static AttachmentType? ForFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return All.FirstOrDefault(type => type.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
    }
}
