using HireLab.Api.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

namespace HireLab.Modules.Sessions.Tests.Realtime;

/// <summary>Records the messages a hub connection receives, in order, and whether it was closed.</summary>
internal sealed class HubInbox
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly Lock _lock = new();
    private readonly List<HubMessage> _messages = [];
    private TaskCompletionSource _arrived = NewSignal();
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private HubInbox(HubConnection connection, Type clientInterface)
    {
        foreach (var clientMethod in clientInterface.GetMethods())
        {
            var method = clientMethod.Name;
            connection.On(method, [.. clientMethod.GetParameters().Select(parameter => parameter.ParameterType)], arguments =>
            {
                lock (_lock)
                {
                    _messages.Add(new HubMessage(method, arguments));
                }

                Interlocked.Exchange(ref _arrived, NewSignal()).TrySetResult();
                return Task.CompletedTask;
            });
        }

        connection.Closed += _ =>
        {
            _closed.TrySetResult();
            return Task.CompletedTask;
        };
    }

    /// <summary>Records what a <c>/hubs/staff</c> connection receives, as <see cref="IStaffClient"/> declares it.</summary>
    public static HubInbox OfStaff(HubConnection connection) => new(connection, typeof(IStaffClient));

    /// <summary>Records what a <c>/hubs/candidate</c> connection receives, as <see cref="ICandidateClient"/> declares it.</summary>
    public static HubInbox OfCandidate(HubConnection connection) => new(connection, typeof(ICandidateClient));

    public IReadOnlyList<HubMessage> Messages
    {
        get
        {
            lock (_lock)
            {
                return [.. _messages];
            }
        }
    }

    /// <summary>Waits until a message to <paramref name="method"/> that matches <paramref name="match"/> arrives and returns its arguments.</summary>
    public async Task<object?[]> WaitForAsync(string method, Func<object?[], bool>? match = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(_timeout);

        while (true)
        {
            var arrived = Volatile.Read(ref _arrived).Task;
            if (Messages.FirstOrDefault(message => message.Method == method && (match?.Invoke(message.Arguments) ?? true)) is { } found)
            {
                return found.Arguments;
            }

            await arrived.WaitAsync(timeout.Token);
        }
    }

    /// <summary>Waits until the server closes the connection.</summary>
    public Task WaitForCloseAsync() => _closed.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);

    public bool IsClosed => _closed.Task.IsCompleted;

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed record HubMessage(string Method, object?[] Arguments);
