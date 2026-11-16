namespace HireLab.Modules.Identity.Bootstrap;

/// <summary>Where staff open the app, bound from <c>HireLab</c>.</summary>
internal sealed class StaffAppOptions
{
    public const string Section = "HireLab";

    /// <summary>
    /// The origin the staff app is served from, for example <c>https://hirelab.example.com</c>. Links printed outside the
    /// app are built on it. Required outside Development.
    /// </summary>
    public Uri? PublicBaseUrl { get; set; }

    public Uri LinkTo(string path)
    {
        var baseUrl = PublicBaseUrl ?? throw new InvalidOperationException(
            $"{Section}:{nameof(PublicBaseUrl)} is not set, so a full link can't be built.");
        return new Uri(baseUrl, path);
    }
}
