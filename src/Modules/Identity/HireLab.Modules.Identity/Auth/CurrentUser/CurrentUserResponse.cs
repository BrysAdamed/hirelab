using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Auth.CurrentUser;

internal sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, StaffRole Role, CurrentUserTeamResponse? Team);

internal sealed record CurrentUserTeamResponse(Guid Id, string Name);
