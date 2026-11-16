using FluentValidation;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Identity.ResetLinks.Redemption;

internal sealed record PreviewResetLinkQuery(string Token)
{
    public override string ToString() => nameof(PreviewResetLinkQuery);
}

/// <summary>Whose password a pending reset link sets, for the redemption page.</summary>
internal sealed record ResetLinkPreviewResponse(string Email, string DisplayName, DateTimeOffset ExpiresAt);

internal sealed class PreviewResetLinkValidator : AbstractValidator<PreviewResetLinkQuery>
{
    public PreviewResetLinkValidator() => RuleFor(query => query.Token).NotEmpty().MaximumLength(OneTimeToken.MaxLength);
}

internal sealed class PreviewResetLinkHandler(IdentityModuleDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<PreviewResetLinkQuery, Result<ResetLinkPreviewResponse>>
{
    public async Task<Result<ResetLinkPreviewResponse>> HandleAsync(PreviewResetLinkQuery query, CancellationToken cancellationToken)
    {
        var found = await dbContext.FindUsableResetLinkAsync(query.Token, timeProvider.GetUtcNow(), cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var (link, user) = found.Value;
        return new ResetLinkPreviewResponse(user.Email!, user.DisplayName, link.ExpiresAt);
    }
}
