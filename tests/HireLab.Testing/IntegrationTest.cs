using HireLab.SharedKernel.Users;
using HireLab.Testing.Staff;
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

    /// <summary>Creates a user in <paramref name="role"/> (with a team unless they are the admin) and signs them in.</summary>
    protected Task<SignedInStaff> SignInAsAsync(StaffRole role) => Host.SignInAsAsync(role);

    /// <summary>Creates an API client that sends <paramref name="staff"/>'s staff cookie.</summary>
    protected HttpClient CreateClient(SignedInStaff staff)
    {
        ArgumentNullException.ThrowIfNull(staff);
        return Host.CreateClient(staff.Cookies);
    }

    /// <summary>Creates a browser-like session, signed in as <paramref name="staff"/> or anonymous, with an antiforgery token.</summary>
    protected Task<BrowserSession> CreateBrowserAsync(SignedInStaff? staff = null) => Host.CreateBrowserAsync(staff?.Cookies);
}
