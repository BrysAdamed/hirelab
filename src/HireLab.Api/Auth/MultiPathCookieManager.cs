using Microsoft.AspNetCore.Authentication.Cookies;

namespace HireLab.Api.Auth;

internal sealed class MultiPathCookieManager(IReadOnlyList<string> paths) : ICookieManager
{
    private readonly ChunkingCookieManager _inner = new();

    public string? GetRequestCookie(HttpContext context, string key) => _inner.GetRequestCookie(context, key);

    public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
    {
        foreach (var path in paths)
        {
            _inner.AppendResponseCookie(context, key, value, WithPath(options, path));
        }
    }

    public void DeleteCookie(HttpContext context, string key, CookieOptions options)
    {
        foreach (var path in paths)
        {
            _inner.DeleteCookie(context, key, WithPath(options, path));
        }
    }

    private static CookieOptions WithPath(CookieOptions options, string path) => new(options) { Path = path };
}
