using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Users;

internal sealed record UserResponse(Guid Id, string Email, string DisplayName, StaffRole Role, UserTeamResponse? Team, bool IsActive);

internal sealed record UserTeamResponse(Guid Id, string Name);

internal static class UserResponseQueries
{
    /// <summary>Projects users to responses with their team's name, ordered by display name.</summary>
    public static IQueryable<UserResponse> ToResponses(this IQueryable<User> users, IdentityModuleDbContext dbContext) =>
        users
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Select(user => new UserResponse(
                user.Id.Value,
                user.Email!,
                user.DisplayName,
                user.Role,
                dbContext.Teams
                    .Where(team => team.Id == user.TeamId)
                    .Select(team => new UserTeamResponse(team.Id.Value, team.Name))
                    .FirstOrDefault(),
                user.IsActive));
}
