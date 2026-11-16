using System.Text;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.Tests.Attachments;

public sealed class AttachmentTypeDetectorTests
{
    public static TheoryData<string, byte[], string> AllowedFiles => new()
    {
        { "cv.pdf", FakeFiles.Pdf, "PDF" },
        { "CV.PDF", FakeFiles.Pdf, "PDF" },
        { "cover-letter.docx", FakeFiles.Docx, "DOCX" },
        { "diagram.png", FakeFiles.Png, "PNG" },
        { "photo.jpg", FakeFiles.Jpeg, "JPG" },
        { "photo.jpeg", FakeFiles.Jpeg, "JPG" },
        { "notes.txt", FakeFiles.Text("Fake notes about nothing."), "TXT" },
        { "notes.txt", [0xEF, 0xBB, 0xBF, .. FakeFiles.Text("With a byte order mark")], "TXT" },
        { "readme.md", FakeFiles.Text("# Fake project\n\nZażółć gęślą jaźń ✓"), "MD" },
    };

    public static TheoryData<string, byte[]> RefusedFiles => new()
    {
        { "program.exe", FakeFiles.Executable },
        { "no-extension", FakeFiles.Pdf },
        { "renamed.pdf", FakeFiles.Executable },
        { "picture-as.pdf", FakeFiles.Png },
        { "pdf-as.png", FakeFiles.Pdf },
        { "png-as.jpg", FakeFiles.Png },
        { "spreadsheet.docx", FakeFiles.Xlsx },
        { "zip-header-only.docx", [0x50, 0x4B, 0x03, 0x04, 0x00, 0x00] },
        { "binary.txt", FakeFiles.Executable },
        { "invalid-utf8.md", [0x23, 0x20, 0xC3, 0x28] },
        { "truncated-utf8.txt", [0x61, 0xE2, 0x82] },
        { "short.pdf", "%PD"u8.ToArray() },
        { "empty.png", [] },
    };

    [Theory]
    [MemberData(nameof(AllowedFiles))]
    public void Allowed_files_are_detected_by_extension_and_content(string fileName, byte[] content, string expectedType)
    {
        // Arrange
        using var stream = new MemoryStream(content);

        // Act
        var type = AttachmentTypeDetector.Detect(fileName, stream);

        // Assert
        type.ShouldNotBeNull().Name.ShouldBe(expectedType);
        stream.Position.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(RefusedFiles))]
    public void Files_with_an_unknown_extension_or_content_that_does_not_match_it_are_refused(string fileName, byte[] content)
    {
        // Arrange
        using var stream = new MemoryStream(content);

        // Act
        var type = AttachmentTypeDetector.Detect(fileName, stream);

        // Assert
        type.ShouldBeNull();
        stream.Position.ShouldBe(0);
    }

    [Fact]
    public void A_text_file_is_checked_beyond_its_first_bytes()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes(new string('a', 200_000) + "\0");
        using var stream = new MemoryStream(content);

        // Act
        var type = AttachmentTypeDetector.Detect("long.txt", stream);

        // Assert
        type.ShouldBeNull();
    }

    [Fact]
    public void A_multibyte_character_across_read_buffers_is_valid_text()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes(new string('a', (64 * 1024) - 1) + "ż and more");
        using var stream = new MemoryStream(content);

        // Act
        var type = AttachmentTypeDetector.Detect("long.txt", stream);

        // Assert
        type.ShouldBe(AttachmentType.Text);
    }

    [Fact]
    public void The_content_type_is_the_detected_type_s()
    {
        // Arrange
        using var stream = new MemoryStream(FakeFiles.Docx);

        // Act
        var type = AttachmentTypeDetector.Detect("cover-letter.docx", stream);

        // Assert
        type.ShouldNotBeNull().ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }
}
