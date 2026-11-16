using System.Diagnostics;
using OpenTelemetry;

namespace HireLab.ServiceDefaults;

/// <summary>
/// Keeps secrets in URLs out of traces: drops query strings, and replaces the token of a candidate link
/// (<c>/s/{token}</c>, ADR 0008), which the request for the candidate app's page carries in its path.
/// </summary>
internal sealed class UrlRedactionProcessor : BaseProcessor<Activity>
{
    private const string CandidateLinkPrefix = "/s/";
    private const string RedactedCandidateLink = "/s/{token}";

    private static readonly string[] _queryTags = ["url.query", "http.target"];
    private static readonly string[] _fullUrlTags = ["url.full", "http.url"];
    private static readonly char[] _notInToken = ['/', '.'];

    public override void OnEnd(Activity data)
    {
        foreach (var tag in _queryTags)
        {
            if (data.GetTagItem(tag) is not null)
            {
                data.SetTag(tag, null);
            }
        }

        foreach (var tag in _fullUrlTags)
        {
            if (data.GetTagItem(tag) is string url)
            {
                data.SetTag(tag, RedactUrl(url));
            }
        }

        if (data.GetTagItem("url.path") is string path)
        {
            data.SetTag("url.path", RedactPath(path));
        }
    }

    internal static string RedactPath(string path) =>
        path.StartsWith(CandidateLinkPrefix, StringComparison.Ordinal)
        && path.Length > CandidateLinkPrefix.Length
        && path.IndexOfAny(_notInToken, CandidateLinkPrefix.Length) < 0
            ? RedactedCandidateLink
            : path;

    internal static string RedactUrl(string url)
    {
        var withoutQuery = url.IndexOf('?', StringComparison.Ordinal) is var index and >= 0 ? url[..index] : url;
        return Uri.TryCreate(withoutQuery, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority) + RedactPath(uri.AbsolutePath)
            : withoutQuery;
    }
}
