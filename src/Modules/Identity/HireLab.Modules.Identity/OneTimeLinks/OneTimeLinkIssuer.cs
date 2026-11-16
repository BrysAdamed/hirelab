using System.Linq.Expressions;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.OneTimeLinks;

/// <summary>A newly issued link and its token, which is shown once and never stored.</summary>
internal sealed record IssuedLink<TLink>(TLink Link, string Token)
    where TLink : OneTimeLink
{
    public string LinkPath => Link.PathFor(Token);

    public override string ToString() => nameof(IssuedLink<TLink>);
}

/// <summary>
/// Issues a one-time link (ADR 0021): in one transaction it revokes the open links the new one replaces and adds the new
/// one. A unique index allows one open link per email or user, so a concurrent request that issues or replaces first
/// makes this one fail with the link's <see cref="OneTimeLink.IssuedMeanwhile"/> conflict.
/// </summary>
internal sealed class OneTimeLinkIssuer(IdentityModuleDbContext dbContext, TimeProvider timeProvider)
{
    /// <param name="link">The new link, created with <paramref name="token"/>.</param>
    /// <param name="token">The new link's token.</param>
    /// <param name="replaces">The links the new one replaces; the open ones are revoked.</param>
    /// <param name="oneOpenIndex">The unique index that allows one open link of this kind.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Result<IssuedLink<TLink>>> IssueAsync<TLink>(
        TLink link,
        OneTimeToken token,
        Expression<Func<TLink, bool>> replaces,
        string oneOpenIndex,
        CancellationToken cancellationToken)
        where TLink : OneTimeLink
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(token);

        var now = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var replaced = await dbContext.Set<TLink>().WhereOpen().Where(replaces).ToListAsync(cancellationToken);
            foreach (var existing in replaced)
            {
                existing.Revoke(now);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Add(link);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException || exception.ViolatedConstraint() == oneOpenIndex)
        {
            return link.IssuedMeanwhile;
        }

        return new IssuedLink<TLink>(link, token.Value);
    }
}
