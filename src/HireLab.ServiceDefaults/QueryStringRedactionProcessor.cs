using System.Diagnostics;
using OpenTelemetry;

namespace HireLab.ServiceDefaults;

internal sealed class QueryStringRedactionProcessor : BaseProcessor<Activity>
{
    private static readonly string[] _queryTags = ["url.query", "http.target"];
    private static readonly string[] _fullUrlTags = ["url.full", "http.url"];

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
            if (data.GetTagItem(tag) is string url && url.IndexOf('?', StringComparison.Ordinal) is var index and >= 0)
            {
                data.SetTag(tag, url[..index]);
            }
        }
    }
}
