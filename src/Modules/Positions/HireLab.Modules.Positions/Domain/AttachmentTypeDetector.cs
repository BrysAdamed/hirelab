using System.IO.Compression;
using System.Text;

namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// Decides the type of an uploaded file (ADR 0005): the extension picks the expected <see cref="AttachmentType"/> and the
/// content must prove it. PDF, PNG and JPG by their magic bytes, DOCX as a ZIP with <c>[Content_Types].xml</c> and a
/// <c>word/</c> part, TXT and MD as valid UTF-8 without NUL bytes. The declared content type is never trusted.
/// </summary>
internal static class AttachmentTypeDetector
{
    private const int TextBufferSize = 64 * 1024;

    private static readonly byte[] _pdf = "%PDF-"u8.ToArray();
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _zip = [0x50, 0x4B, 0x03, 0x04];

    /// <summary>
    /// The type of <paramref name="content"/> named <paramref name="fileName"/>, or <c>null</c> when the extension isn't
    /// allowed or the content doesn't match it. <paramref name="content"/> must be seekable; it is left at the start.
    /// </summary>
    public static AttachmentType? Detect(string fileName, Stream content)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanSeek)
        {
            throw new ArgumentException("The content must be seekable.", nameof(content));
        }

        var type = AttachmentType.ForFileName(fileName);
        if (type is null)
        {
            return null;
        }

        try
        {
            content.Position = 0;
            return Matches(type, content) ? type : null;
        }
        finally
        {
            content.Position = 0;
        }
    }

    private static bool Matches(AttachmentType type, Stream content)
    {
        if (type == AttachmentType.Pdf)
        {
            return StartsWith(content, _pdf);
        }

        if (type == AttachmentType.Png)
        {
            return StartsWith(content, _png);
        }

        if (type == AttachmentType.Jpeg)
        {
            return StartsWith(content, _jpeg);
        }

        if (type == AttachmentType.Docx)
        {
            return StartsWith(content, _zip) && IsWordDocument(content);
        }

        return IsUtf8TextWithoutNul(content);
    }

    private static bool StartsWith(Stream content, byte[] signature)
    {
        content.Position = 0;
        Span<byte> header = stackalloc byte[signature.Length];
        return content.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length
            && header.SequenceEqual(signature);
    }

    private static bool IsWordDocument(Stream content)
    {
        content.Position = 0;
        try
        {
            using var zip = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            var hasContentTypes = false;
            var hasWordPart = false;
            foreach (var entry in zip.Entries)
            {
                hasContentTypes |= entry.FullName == "[Content_Types].xml";
                hasWordPart |= entry.FullName.StartsWith("word/", StringComparison.Ordinal);
            }

            return hasContentTypes && hasWordPart;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool IsUtf8TextWithoutNul(Stream content)
    {
        content.Position = 0;
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var decoder = encoding.GetDecoder();
        var buffer = new byte[TextBufferSize];
        var chars = new char[encoding.GetMaxCharCount(TextBufferSize)];
        try
        {
            int read;
            while ((read = content.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (buffer.AsSpan(0, read).Contains((byte)0))
                {
                    return false;
                }

                decoder.GetChars(buffer, 0, read, chars, 0, flush: false);
            }

            decoder.GetChars([], 0, 0, chars, 0, flush: true);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
