namespace HireLab.Modules.Identity.Invites;

internal static class InviteLinks
{
    /// <summary>
    /// The staff app page that redeems an invite. The token travels in the fragment, so it never reaches a server log or a
    /// <c>Referer</c> header; the page posts it to the API in the request body.
    /// </summary>
    public static string PathFor(string token) => $"/invite#{token}";
}
