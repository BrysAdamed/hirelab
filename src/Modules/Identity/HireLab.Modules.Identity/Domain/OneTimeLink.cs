using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Identity.Domain;

/// <summary>
/// A link sent by hand that works once until it expires (ADR 0007, ADR 0021): invite links and reset links. It stores only
/// the hash of its <see cref="OneTimeToken"/>. Its state is derived from the clock: Pending, Used, Revoked or Expired.
/// </summary>
internal abstract class OneTimeLink
{
    protected OneTimeLink()
    {
    }

    protected OneTimeLink(OneTimeToken token, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(token);

        TokenHash = token.Hash;
        ExpiresAt = now + lifetime;
    }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public uint Version { get; private set; }

    /// <summary>Neither used nor revoked. An open link may still have expired.</summary>
    public bool IsOpen => UsedAt is null && RevokedAt is null;

    /// <summary>The prefix of this link's error codes, for example <c>Invite</c>.</summary>
    protected abstract string ErrorPrefix { get; }

    /// <summary>How the link is called in messages, for example <c>invite link</c>.</summary>
    protected abstract string LinkName { get; }

    /// <summary>The staff app page that redeems the link, for example <c>invite</c>.</summary>
    protected abstract string RedemptionPage { get; }

    /// <summary>Another request issued a link replacing the same open links first.</summary>
    public Error IssuedMeanwhile => Error.Conflict(
        $"{ErrorPrefix}.IssuedMeanwhile",
        $"Another {LinkName} for the same person was issued at the same time. Reload and try again.");

    /// <summary>
    /// The path of the staff app page that redeems the link. The token travels in the fragment, so it never reaches a
    /// server log or a <c>Referer</c> header; the page posts it to the API in the request body.
    /// </summary>
    public string PathFor(string token) => $"/{RedemptionPage}#{token}";

    public OneTimeLinkState StateAt(DateTimeOffset now) => this switch
    {
        { UsedAt: not null } => OneTimeLinkState.Used,
        { RevokedAt: not null } => OneTimeLinkState.Revoked,
        _ when now >= ExpiresAt => OneTimeLinkState.Expired,
        _ => OneTimeLinkState.Pending,
    };

    /// <summary>Succeeds while the link is pending; otherwise says why it can't be used.</summary>
    public Result EnsureUsable(DateTimeOffset now) => StateAt(now) switch
    {
        OneTimeLinkState.Pending => Result.Success(),
        OneTimeLinkState.Used => Error.Conflict($"{ErrorPrefix}.AlreadyUsed", $"This {LinkName} has already been used."),
        OneTimeLinkState.Revoked => Error.Conflict($"{ErrorPrefix}.Revoked", $"This {LinkName} has been revoked. Ask for a new one."),
        _ => Error.Conflict($"{ErrorPrefix}.Expired", $"This {LinkName} has expired. Ask for a new one."),
    };

    public Result Use(DateTimeOffset now)
    {
        var usable = EnsureUsable(now);
        if (usable.IsSuccess)
        {
            UsedAt = now;
        }

        return usable;
    }

    /// <summary>Revokes an open link, pending or expired. A used or revoked link can't be revoked.</summary>
    public Result Revoke(DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return Error.Conflict($"{ErrorPrefix}.NotOpen", $"This {LinkName} has already been used or revoked.");
        }

        RevokedAt = now;
        return Result.Success();
    }
}
