using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions;

internal sealed class StaffSessionAccess(SessionsDbContext dbContext) : IStaffSessionAccess
{
    public Task<bool> CanWatchAsync(InterviewSessionId sessionId, CancellationToken cancellationToken = default) =>
        dbContext.Sessions.AnyAsync(session => session.Id == sessionId, cancellationToken);
}
