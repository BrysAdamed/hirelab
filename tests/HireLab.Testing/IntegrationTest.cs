using Xunit;

namespace HireLab.Testing;

/// <summary>Base class of integration tests on the shared <see cref="HireLabTestHost"/>. Resets the database before each test.</summary>
public abstract class IntegrationTest(HireLabTestHost host) : IAsyncLifetime
{
    /// <summary>The test assembly's shared host.</summary>
    protected HireLabTestHost Host { get; } = host;

    /// <summary>The current test's cancellation token.</summary>
    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <inheritdoc />
    public virtual ValueTask InitializeAsync() => Host.ResetAsync();

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
