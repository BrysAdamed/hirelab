namespace HireLab.Modules.Identity.ResetLinks;

internal static class ResetLinkPaths
{
    /// <summary>
    /// The staff app page that redeems a reset link. The token travels in the fragment, so it never reaches a server log
    /// or a <c>Referer</c> header; the page posts it to the API in the request body.
    /// </summary>
    public static string PathFor(string token) => $"/reset#{token}";
}
