using System.Collections.Concurrent;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Testing;

/// <summary>
/// Lets a test say that a candidate has an unfinished interview session without running one: the API's
/// <see cref="IUnfinishedSessionsQuery"/> reports the candidates marked here and asks the real implementation about
/// the rest.
/// </summary>
public sealed class UnfinishedSessions
{
    private readonly ConcurrentDictionary<CandidateId, bool> _candidates = new();

    /// <summary>Makes the API see an unfinished session for <paramref name="candidateId"/> until the next reset.</summary>
    public void Add(CandidateId candidateId) => _candidates[candidateId] = true;

    /// <summary>Forgets every marked candidate.</summary>
    public void Reset() => _candidates.Clear();

    internal static void Decorate(IServiceCollection services, UnfinishedSessions unfinishedSessions)
    {
        var descriptor = services.Single(descriptor => descriptor.ServiceType == typeof(IUnfinishedSessionsQuery));
        var implementationType = descriptor.ImplementationType
            ?? throw new InvalidOperationException("IUnfinishedSessionsQuery must be registered by type to be decorated.");

        services.Remove(descriptor);
        services.Add(new ServiceDescriptor(implementationType, implementationType, descriptor.Lifetime));
        services.Add(new ServiceDescriptor(
            typeof(IUnfinishedSessionsQuery),
            provider => new Query((IUnfinishedSessionsQuery)provider.GetRequiredService(implementationType), unfinishedSessions),
            descriptor.Lifetime));
    }

    private sealed class Query(IUnfinishedSessionsQuery inner, UnfinishedSessions unfinishedSessions) : IUnfinishedSessionsQuery
    {
        public Task<bool> AnyForCandidatesAsync(IReadOnlyCollection<CandidateId> candidateIds, CancellationToken cancellationToken = default) =>
            candidateIds.Any(unfinishedSessions._candidates.ContainsKey)
                ? Task.FromResult(true)
                : inner.AnyForCandidatesAsync(candidateIds, cancellationToken);
    }
}
