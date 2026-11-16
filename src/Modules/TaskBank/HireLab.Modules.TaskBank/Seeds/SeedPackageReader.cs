using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HireLab.Modules.TaskBank.Domain;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>
/// Reads seed packages (ADR 0030): groups the files into <c>&lt;slug&gt;/v&lt;N&gt;/</c> folders, checks that each task's
/// versions run from v1 without gaps, inlines every <c>{ "$file": "path" }</c> in <c>task.json</c> with that file's text
/// (UTF-8, line endings normalised to LF) and maps the result to task version details. A package may contain only
/// <c>task.json</c> and the files it references, and never a <c>draft</c> field: seeds are always published.
/// </summary>
internal static partial class SeedPackageReader
{
    public const string TaskFileName = "task.json";
    public const string FileReferenceProperty = "$file";
    public const string DraftProperty = "draft";

    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string FolderOf(string slug, int number) => $"{slug}/v{number.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Reads every package, ordered by slug and version number. Throws <see cref="SeedPackageException"/> on the first broken rule.</summary>
    public static IReadOnlyList<SeedTaskPackage> Read(IEnumerable<SeedPackageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var packages = files
            .Select(file => (Location: Locate(file.Path), File: file))
            .GroupBy(entry => (entry.Location.Slug, entry.Location.Number))
            .Select(package => ReadPackage(
                package.Key.Slug,
                package.Key.Number,
                package.Select(entry => entry.File with { Path = entry.Location.RelativePath }).ToList()))
            .OrderBy(package => package.Slug, StringComparer.Ordinal)
            .ThenBy(package => package.Number)
            .ToList();

        EnsureContiguousVersions(packages);
        return packages;
    }

    private static (string Slug, int Number, string RelativePath) Locate(string path)
    {
        var segments = path.Split('/', 3);
        if (segments.Length < 3
            || !KebabCase.IsMatch(segments[0])
            || !VersionFolder().IsMatch(segments[1])
            || !int.TryParse(segments[1].AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            throw new SeedPackageException(
                $"The file '{path}' is not inside a seed package folder '<slug>/v<N>/' with a kebab-case slug and a version number from 1.");
        }

        return (segments[0], number, segments[2]);
    }

    private static SeedTaskPackage ReadPackage(string slug, int number, IReadOnlyList<SeedPackageFile> files)
    {
        var name = FolderOf(slug, number);
        var filesByPath = files.ToDictionary(file => file.Path, StringComparer.Ordinal);
        if (!filesByPath.TryGetValue(TaskFileName, out var taskFile))
        {
            throw new SeedPackageException($"Seed package {name} has no {TaskFileName}.");
        }

        var root = ParseTaskFile(name, taskFile);
        if (root.ContainsKey(DraftProperty))
        {
            throw new SeedPackageException(
                $"Seed package {name} has a '{DraftProperty}' field. Seed packages are always published; remove it.");
        }

        var referenced = new HashSet<string>(StringComparer.Ordinal);
        var inlined = Inline(root, new PackageFiles(name, filesByPath, referenced));

        var unreferenced = filesByPath.Keys
            .Where(path => path != TaskFileName && !referenced.Contains(path))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (unreferenced.Count > 0)
        {
            throw new SeedPackageException(
                $"Seed package {name} has files that {TaskFileName} doesn't reference: {string.Join(", ", unreferenced)}.");
        }

        SeedTaskDocument document;
        try
        {
            document = inlined.Deserialize<SeedTaskDocument>(TaskTypeJson.Options)
                ?? throw new SeedPackageException($"{name}/{TaskFileName} must be a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new SeedPackageException($"{name}/{TaskFileName} doesn't match the seed package format: {exception.Message}", exception);
        }

        return new SeedTaskPackage(slug, number, document.TaskType, document.ToDetails(), files);
    }

    private static JsonObject ParseTaskFile(string name, SeedPackageFile taskFile)
    {
        try
        {
            return JsonNode.Parse(taskFile.Content) as JsonObject
                ?? throw new SeedPackageException($"{name}/{TaskFileName} must be a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new SeedPackageException($"{name}/{TaskFileName} is not valid JSON: {exception.Message}", exception);
        }
    }

    private static JsonNode? Inline(JsonNode? node, PackageFiles files)
    {
        switch (node)
        {
            case JsonObject reference when reference.ContainsKey(FileReferenceProperty):
                return JsonValue.Create(files.ReadReferenced(reference));
            case JsonObject jsonObject:
                foreach (var (key, value) in jsonObject.ToList())
                {
                    var inlined = Inline(value, files);
                    if (!ReferenceEquals(inlined, value))
                    {
                        jsonObject[key] = inlined;
                    }
                }

                return jsonObject;
            case JsonArray array:
                for (var index = 0; index < array.Count; index++)
                {
                    var value = array[index];
                    var inlined = Inline(value, files);
                    if (!ReferenceEquals(inlined, value))
                    {
                        array[index] = inlined;
                    }
                }

                return array;
            default:
                return node;
        }
    }

    private static void EnsureContiguousVersions(IReadOnlyList<SeedTaskPackage> packages)
    {
        foreach (var task in packages.GroupBy(package => package.Slug))
        {
            var numbers = task.Select(package => package.Number).ToList();
            if (!numbers.SequenceEqual(Enumerable.Range(1, numbers.Count)))
            {
                var found = string.Join(", ", numbers.Select(number => $"v{number.ToString(CultureInfo.InvariantCulture)}"));
                throw new SeedPackageException(
                    $"Seed task {task.Key} has the versions {found}. Number them from v1 without gaps.");
            }
        }
    }

    [GeneratedRegex("^v[1-9][0-9]{0,8}$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionFolder();

    private sealed class PackageFiles(string name, IReadOnlyDictionary<string, SeedPackageFile> files, HashSet<string> referenced)
    {
        public string ReadReferenced(JsonObject reference)
        {
            if (reference.Count != 1
                || reference[FileReferenceProperty] is not JsonValue value
                || !value.TryGetValue<string>(out var path))
            {
                throw new SeedPackageException(
                    $"Seed package {name} has a file reference that isn't {{ \"{FileReferenceProperty}\": \"<path>\" }} alone.");
            }

            if (!IsPackagePath(path) || path == TaskFileName)
            {
                throw new SeedPackageException(
                    $"Seed package {name} references '{path}'. Use a path relative to the package folder, with '/' and no '.' or '..' segments.");
            }

            if (!files.TryGetValue(path, out var file))
            {
                throw new SeedPackageException($"Seed package {name} references the missing file '{path}'.");
            }

            referenced.Add(path);
            return ReadText(file);
        }

        private string ReadText(SeedPackageFile file)
        {
            string text;
            try
            {
                text = _strictUtf8.GetString(file.Content);
            }
            catch (DecoderFallbackException exception)
            {
                throw new SeedPackageException($"Seed package {name} has the file '{file.Path}', which isn't UTF-8 text.", exception);
            }

            return text.TrimStart('﻿').Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        private static bool IsPackagePath(string path) =>
            path.Length > 0
            && !path.Contains('\\', StringComparison.Ordinal)
            && path.Split('/').All(segment => segment is not ("" or "." or ".."));
    }
}
