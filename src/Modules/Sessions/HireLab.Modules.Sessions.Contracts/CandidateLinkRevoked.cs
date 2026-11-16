using HireLab.SharedKernel.Events;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Raised after a session's candidate link was regenerated (ADR 0031): <see cref="CandidateLinkId"/> is the replaced
/// link, whose token and cookies no longer work. The candidate hub drops the connections opened with it (ADR 0008).
/// </summary>
public sealed record CandidateLinkRevoked(InterviewSessionId SessionId, CandidateLinkId CandidateLinkId) : IIntegrationEvent;
