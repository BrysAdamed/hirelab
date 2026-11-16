using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.RevokeInvite;

internal sealed record RevokeInviteCommand(InviteId InviteId, InviteScope Scope);

internal sealed class RevokeInviteHandler(IdentityModuleDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<RevokeInviteCommand, Result>
{
    public async Task<Result> HandleAsync(RevokeInviteCommand command, CancellationToken cancellationToken)
    {
        var invite = await command.Scope.Apply(dbContext.Invites)
            .SingleOrDefaultAsync(invite => invite.Id == command.InviteId, cancellationToken);

        return invite is null ? InviteErrors.NotFound : invite.Revoke(timeProvider.GetUtcNow());
    }
}
