using System.IO.Compression;
using System.Text;

namespace HireLab.Modules.Positions.Tests.Attachments;

/// <summary>Tiny made-up files whose first bytes are what the attachment type checks look for.</summary>
internal static class FakeFiles
{
    public static byte[] Pdf => "%PDF-1.4\n% Fake CV of Jane Fakeperson\n%%EOF\n"u8.ToArray();

    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00];

    public static byte[] Executable => "MZ\u0090\0\u0003\0\0\0fake program"u8.ToArray();

    public static byte[] Docx => Zip("[Content_Types].xml", "word/document.xml");

    public static byte[] Xlsx => Zip("[Content_Types].xml", "xl/workbook.xml");

    public static byte[] Text(string content) => Encoding.UTF8.GetBytes(content);

    /// <summary>A PDF padded to exactly <paramref name="size"/> bytes.</summary>
    public static byte[] PdfOfSize(long size)
    {
        var content = new byte[size];
        Pdf.CopyTo(content, 0);
        content.AsSpan(Pdf.Length).Fill((byte)' ');
        return content;
    }

    private static byte[] Zip(params string[] entryNames)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in entryNames)
            {
                using var entry = new StreamWriter(zip.CreateEntry(name).Open());
                entry.Write("<fake />");
            }
        }

        return stream.ToArray();
    }
}
