namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// What a valid candidate cookie gives access to (ADR 0008): the session, and the candidate link the cookie was
/// exchanged for. The candidate hub groups the candidate's connections by them.
/// </summary>
public sealed record CandidateSessionAccess(InterviewSessionId SessionId, CandidateLinkId CandidateLinkId);
