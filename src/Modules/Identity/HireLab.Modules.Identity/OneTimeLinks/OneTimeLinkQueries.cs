using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.OneTimeLinks;

internal static class OneTimeLinkQueries
{
    /// <summary>The links that are neither used nor revoked, expired or not.</summary>
    public static IQueryable<TLink> WhereOpen<TLink>(this IQueryable<TLink> links)
        where TLink : OneTimeLink =>
        links.Where(link => link.UsedAt == null && link.RevokedAt == null);

    /// <summary>The links that still work at <paramref name="now"/>.</summary>
    public static IQueryable<TLink> WherePendingAt<TLink>(this IQueryable<TLink> links, DateTimeOffset now)
        where TLink : OneTimeLink =>
        links.WhereOpen().Where(link => link.ExpiresAt > now);

    /// <summary>Revokes the pending links, to be saved with the change that makes them unsafe.</summary>
    public static async Task RevokePendingAsync<TLink>(this IQueryable<TLink> links, DateTimeOffset now, CancellationToken cancellationToken)
        where TLink : OneTimeLink
    {
        foreach (var link in await links.WherePendingAt(now).ToListAsync(cancellationToken))
        {
            link.Revoke(now);
        }
    }

    /// <summary>Finds the link by its token and checks it can still be used; <paramref name="notFound"/> when no link has it.</summary>
    public static async Task<Result<TLink>> FindUsableAsync<TLink>(
        this IQueryable<TLink> links,
        string token,
        DateTimeOffset now,
        Error notFound,
        CancellationToken cancellationToken)
        where TLink : OneTimeLink
    {
        var hash = OneTimeToken.HashOf(token);
        var link = await links.SingleOrDefaultAsync(link => link.TokenHash == hash, cancellationToken);
        if (link is null)
        {
            return notFound;
        }

        var usable = link.EnsureUsable(now);
        return usable.IsSuccess ? link : usable.Error;
    }
}
