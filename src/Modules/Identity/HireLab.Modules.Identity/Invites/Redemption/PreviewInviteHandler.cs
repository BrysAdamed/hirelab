using FluentValidation;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.Redemption;

internal sealed record PreviewInviteQuery(string Token)
{
    public override string ToString() => nameof(PreviewInviteQuery);
}

/// <summary>What the redemption page shows before the invitee sets a password.</summary>
internal sealed record InvitePreviewResponse(
    string Email,
    string DisplayName,
    StaffRole Role,
    string? TeamName,
    bool IsNewTeam,
    DateTimeOffset ExpiresAt);

internal sealed class PreviewInviteValidator : AbstractValidator<PreviewInviteQuery>
{
    public PreviewInviteValidator() => RuleFor(query => query.Token).NotEmpty().MaximumLength(OneTimeToken.MaxLength);
}

internal sealed class PreviewInviteHandler(IdentityModuleDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<PreviewInviteQuery, Result<InvitePreviewResponse>>
{
    public async Task<Result<InvitePreviewResponse>> HandleAsync(PreviewInviteQuery query, CancellationToken cancellationToken)
    {
        var found = await dbContext.Invites.FindUsableAsync(query.Token, timeProvider.GetUtcNow(), InviteErrors.LinkNotFound, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var invite = found.Value;
        var teamName = invite.TeamId is { } teamId
            ? await dbContext.Teams.Where(team => team.Id == teamId).Select(team => team.Name).SingleAsync(cancellationToken)
            : invite.NewTeamName;

        return new InvitePreviewResponse(
            invite.Email,
            invite.DisplayName,
            invite.Role,
            teamName,
            invite.NewTeamName is not null,
            invite.ExpiresAt);
    }
}
