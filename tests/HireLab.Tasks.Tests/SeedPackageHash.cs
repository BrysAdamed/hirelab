using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using HireLab.Modules.TaskBank.Seeds;

namespace HireLab.Tasks.Tests;

/// <summary>
/// The hash <c>tasks.lock.json</c> holds per seed package (ADR 0030): SHA-256, as lowercase hex, over the package's files
/// in ordinal order of their path relative to the package folder. Each file adds its path in UTF-8, a NUL byte, the
/// length of its content as an 8-byte big-endian number, and its content with CRLF line endings turned into LF.
/// </summary>
internal static class SeedPackageHash
{
    public static string Compute(IEnumerable<SeedPackageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(long)];
        foreach (var file in files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var content = NormaliseLineEndings(file.Content);
            hash.AppendData(Encoding.UTF8.GetBytes(file.Path));
            hash.AppendData([0]);
            BinaryPrimitives.WriteInt64BigEndian(length, content.Length);
            hash.AppendData(length);
            hash.AppendData(content);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static byte[] NormaliseLineEndings(byte[] content)
    {
        var normalised = new List<byte>(content.Length);
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
            {
                continue;
            }

            normalised.Add(content[index]);
        }

        return [.. normalised];
    }
}
