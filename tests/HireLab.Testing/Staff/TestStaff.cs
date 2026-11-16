using HireLab.SharedKernel.Users;
using Microsoft.Net.Http.Headers;

namespace HireLab.Testing.Staff;

/// <summary>A persisted test team.</summary>
public sealed record TestTeam(TeamId Id, string Name);

/// <summary>A persisted test user and the password they can log in with.</summary>
public sealed record TestUser(UserId Id, string Email, string DisplayName, StaffRole Role, TeamId? TeamId, string Password);

/// <summary>A test user signed in with the staff cookie scheme, and the cookies the sign-in issued.</summary>
public sealed record SignedInStaff(TestUser User, IReadOnlyList<SetCookieHeaderValue> Cookies);
