using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.ResetLinks.GenerateResetLink;

internal sealed record GenerateResetLinkCommand(UserId UserId, ResetLinkScope Scope);

/// <summary>A new reset link and the path of its link, shown once: only a hash of the token is stored.</summary>
internal sealed record IssuedResetLinkResponse(Guid UserId, DateTimeOffset ExpiresAt, string LinkPath);

internal sealed class GenerateResetLinkHandler(IdentityModuleDbContext dbContext, ResetLinkIssuer issuer, ICurrentUser currentUser)
    : ICommandHandler<GenerateResetLinkCommand, Result<IssuedResetLinkResponse>>
{
    public async Task<Result<IssuedResetLinkResponse>> HandleAsync(GenerateResetLinkCommand command, CancellationToken cancellationToken)
    {
        var inScope = await command.Scope.Apply(dbContext.Users).AnyAsync(user => user.Id == command.UserId, cancellationToken);
        if (!inScope)
        {
            return ResetLinkErrors.UserNotFound;
        }

        var issued = await issuer.IssueAsync(command.UserId, currentUser.Id, cancellationToken);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        return new IssuedResetLinkResponse(command.UserId.Value, issued.Value.Link.ExpiresAt, issued.Value.LinkPath);
    }
}
