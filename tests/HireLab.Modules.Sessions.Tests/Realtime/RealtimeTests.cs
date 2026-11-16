using System.Net;
using HireLab.Api.Realtime;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Tests.CandidateAccess;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Sessions;
using HireLab.Testing.Staff;
using HireLab.Testing.TaskBank;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace HireLab.Modules.Sessions.Tests.Realtime;

public sealed class RealtimeTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string SubscribeToSession = nameof(StaffHubMethods.SubscribeToSession);

    [Theory]
    [InlineData(HttpTransportType.WebSockets)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.LongPolling)]
    public async Task Staff_of_the_team_subscribe_to_a_session_and_get_its_events(HttpTransportType transport)
    {
        // Arrange
        var session = await PlannedSessionAsync();
        await using var connection = Host.CreateStaffHubConnection(session.Interviewer, transport);
        var inbox = HubInbox.OfStaff(connection);
        await connection.StartAsync(CancellationToken);

        // Act
        var presence = await connection.InvokeAsync<CandidatePresence>(SubscribeToSession, session.Id.Value, CancellationToken);
        await session.Staff.WentLiveAsync(session.Id);

        // Assert
        presence.ShouldBe(new CandidatePresence(false, 0, null));
        (await inbox.WaitForAsync(nameof(IStaffClient.SessionStateChanged))).ShouldBe([session.Id.Value]);
        var queue = await session.Staff.SessionAsync(session.Id);
        (await inbox.WaitForAsync(nameof(IStaffClient.TaskQueueChanged))).ShouldBe([session.Id.Value, queue.QueueRevision]);
    }

    [Fact]
    public async Task Any_number_of_team_members_watch_the_same_session()
    {
        // Arrange
        var session = await PlannedSessionAsync();
        var colleague = await SignInAsColleagueAsync(session.Interviewer);
        await using var first = await SubscribedStaffAsync(session.Interviewer, session.Id);
        await using var second = await SubscribedStaffAsync(colleague, session.Id);

        // Act
        await session.Staff.WentLiveAsync(session.Id);

        // Assert
        await first.Inbox.WaitForAsync(nameof(IStaffClient.SessionStateChanged));
        await second.Inbox.WaitForAsync(nameof(IStaffClient.SessionStateChanged));
    }

    [Fact]
    public async Task Subscribing_to_another_teams_session_is_refused()
    {
        // Arrange
        var session = await PlannedSessionAsync();
        var outsider = await SignInAsAsync(StaffRole.Interviewer);
        await using var connection = Host.CreateStaffHubConnection(outsider);
        await connection.StartAsync(CancellationToken);

        // Act
        var subscribing = connection.InvokeAsync<CandidatePresence>(SubscribeToSession, session.Id.Value, CancellationToken);

        // Assert
        (await Should.ThrowAsync<HubException>(subscribing)).Message.ShouldContain("The session was not found.");
    }

    [Fact]
    public async Task Subscribing_to_an_unknown_session_is_refused()
    {
        // Arrange
        var interviewer = await SignInAsAsync(StaffRole.Interviewer);
        await using var connection = Host.CreateStaffHubConnection(interviewer);
        await connection.StartAsync(CancellationToken);

        // Act
        var subscribing = connection.InvokeAsync<CandidatePresence>(SubscribeToSession, Guid.CreateVersion7(), CancellationToken);

        // Assert
        (await Should.ThrowAsync<HubException>(subscribing)).Message.ShouldContain("The session was not found.");
    }

    [Fact]
    public async Task The_admin_cannot_connect_to_the_staff_hub()
    {
        // Arrange
        var admin = await SignInAsAsync(StaffRole.Admin);
        await using var connection = Host.CreateStaffHubConnection(admin);

        // Act
        var starting = connection.StartAsync(CancellationToken);

        // Assert
        (await Should.ThrowAsync<HttpRequestException>(starting)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_subscriber_gets_only_the_events_of_its_own_session()
    {
        // Arrange
        var session = await PlannedSessionAsync();
        var otherSessionId = (await session.Staff.CreatedAsync(await Host.CreateCandidateAsync(session.TeamId))).SessionId;
        await session.Staff.AddedTaskAsync(otherSessionId, await Host.AddBankTaskAsync(session.TeamId));
        await using var subscriber = await SubscribedStaffAsync(session.Interviewer, session.Id);

        // Act
        await session.Staff.WentLiveAsync(otherSessionId);
        await session.Staff.WentLiveAsync(session.Id);

        // Assert
        await subscriber.Inbox.WaitForAsync(nameof(IStaffClient.SessionStateChanged));
        subscriber.Inbox.Messages.ShouldAllBe(message => (Guid)message.Arguments[0]! == session.Id.Value);
    }

    [Fact]
    public async Task Unsubscribing_stops_the_events()
    {
        // Arrange
        var session = await PlannedSessionAsync();
        var otherSessionId = (await session.Staff.CreatedAsync(await Host.CreateCandidateAsync(session.TeamId))).SessionId;
        await session.Staff.AddedTaskAsync(otherSessionId, await Host.AddBankTaskAsync(session.TeamId));
        await using var subscriber = await SubscribedStaffAsync(session.Interviewer, session.Id);
        await subscriber.Connection.InvokeAsync<CandidatePresence>(SubscribeToSession, otherSessionId.Value, CancellationToken);

        // Act
        await subscriber.Connection.InvokeAsync(nameof(StaffHubMethods.UnsubscribeFromSession), session.Id.Value, CancellationToken);
        await session.Staff.WentLiveAsync(session.Id);
        await session.Staff.WentLiveAsync(otherSessionId);

        // Assert
        await subscriber.Inbox.WaitForAsync(nameof(IStaffClient.SessionStateChanged));
        subscriber.Inbox.Messages.ShouldAllBe(message => (Guid)message.Arguments[0]! == otherSessionId.Value);
    }

    [Fact]
    public async Task Staff_and_the_candidate_get_queue_changes()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await using var staff = await SubscribedStaffAsync(session.Interviewer, session.Id);
        await using var candidate = await ConnectedCandidateAsync(session.Candidate, session.Id);

        // Act
        await session.Staff.MarkedDoneAsync(session.Id, session.First);

        // Assert
        var queue = await session.Staff.SessionAsync(session.Id);
        (await staff.Inbox.WaitForAsync(nameof(IStaffClient.TaskQueueChanged))).ShouldBe([session.Id.Value, queue.QueueRevision]);
        (await candidate.Inbox.WaitForAsync(nameof(ICandidateClient.TaskQueueChanged))).ShouldBeEmpty();
    }

    [Fact]
    public async Task Staff_and_the_candidate_get_response_updates()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await using var staff = await SubscribedStaffAsync(session.Interviewer, session.Id);
        await using var candidate = await ConnectedCandidateAsync(session.Candidate, session.Id);

        // Act
        await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake answer" }, 0);

        // Assert
        (await staff.Inbox.WaitForAsync(nameof(IStaffClient.ResponseUpdated))).ShouldBe([session.Id.Value, session.First, 1]);
        (await candidate.Inbox.WaitForAsync(nameof(ICandidateClient.ResponseUpdated))).ShouldBe([session.First, 1]);
    }

    [Fact]
    public async Task A_candidate_gets_only_the_events_of_their_own_session()
    {
        // Arrange
        var session = await LiveSessionAsync();
        var other = await LiveSessionAsync();
        await using var candidate = await ConnectedCandidateAsync(session.Candidate, session.Id);

        // Act
        await other.Staff.MarkedDoneAsync(other.Id, other.First);
        await session.Staff.MarkedDoneAsync(session.Id, session.First);

        // Assert
        await candidate.Inbox.WaitForAsync(nameof(ICandidateClient.TaskQueueChanged));
        candidate.Inbox.Messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Staff_see_the_candidate_connect_and_disconnect()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await using var staff = await SubscribedStaffAsync(session.Interviewer, session.Id);
        var candidate = await ConnectedCandidateAsync(session.Candidate, session.Id);
        var connectedAt = Host.Time.GetUtcNow();
        var connected = await staff.Inbox.WaitForAsync(nameof(IStaffClient.CandidatePresenceChanged));
        Host.Time.Advance(TimeSpan.FromMinutes(3));

        // Act
        await candidate.DisposeAsync();

        // Assert
        connected.ShouldBe([session.Id.Value, new CandidatePresence(true, 1, connectedAt)]);
        var disconnected = await staff.Inbox.WaitForAsync(
            nameof(IStaffClient.CandidatePresenceChanged),
            arguments => !((CandidatePresence)arguments[1]!).Connected);
        disconnected.ShouldBe([session.Id.Value, new CandidatePresence(false, 0, connectedAt.AddMinutes(3))]);
    }

    [Fact]
    public async Task Subscribing_returns_the_presence_of_the_candidates_open_connections()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await using var firstTab = await ConnectedCandidateAsync(session.Candidate, session.Id);
        await using var secondTab = await ConnectedCandidateAsync(session.Candidate, session.Id);

        // Act
        await using var staff = Host.CreateStaffHubConnection(session.Interviewer);
        await staff.StartAsync(CancellationToken);
        var presence = await staff.InvokeAsync<CandidatePresence>(SubscribeToSession, session.Id.Value, CancellationToken);

        // Assert
        presence.ShouldBe(new CandidatePresence(true, 2, Host.Time.GetUtcNow()));
    }

    [Fact]
    public async Task A_candidate_without_a_cookie_cannot_connect()
    {
        // Arrange
        using var anonymous = Host.CreateBrowser();
        await using var connection = Host.CreateCandidateHubConnection(anonymous);

        // Act
        var starting = connection.StartAsync(CancellationToken);

        // Assert
        (await Should.ThrowAsync<HttpRequestException>(starting)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_cookie_of_a_regenerated_link_cannot_connect()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await session.Staff.IssuedCandidateLinkAsync(session.Id.Value);
        await using var connection = Host.CreateCandidateHubConnection(session.Candidate);

        // Act
        var starting = connection.StartAsync(CancellationToken);

        // Assert
        (await Should.ThrowAsync<HttpRequestException>(starting)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Regenerating_the_link_drops_the_old_links_connections()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await using var oldLink = await ConnectedCandidateAsync(session.Candidate, session.Id);
        await using var staff = await SubscribedStaffAsync(session.Interviewer, session.Id);

        // Act
        var token = await session.Staff.IssuedCandidateLinkAsync(session.Id.Value);

        // Assert
        await oldLink.Inbox.WaitForCloseAsync();
        await staff.Inbox.WaitForAsync(
            nameof(IStaffClient.CandidatePresenceChanged),
            arguments => ((CandidatePresence)arguments[1]!).Connections == 0);
        using var newCandidate = await Host.CreateCandidateBrowserAsync();
        await newCandidate.ExchangedAsync(token);
        await using var newLink = await ConnectedCandidateAsync(newCandidate, session.Id);
        newLink.Inbox.IsClosed.ShouldBeFalse();
    }

    [Fact]
    public async Task Finishing_tells_staff_and_the_candidate_and_drops_the_candidates_connections()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await using var staff = await SubscribedStaffAsync(session.Interviewer, session.Id);
        await using var candidate = await ConnectedCandidateAsync(session.Candidate, session.Id);

        // Act
        await session.Staff.FinishedAsync(session.Id);

        // Assert
        (await staff.Inbox.WaitForAsync(nameof(IStaffClient.SessionStateChanged))).ShouldBe([session.Id.Value]);
        await candidate.Inbox.WaitForCloseAsync();
        await staff.Inbox.WaitForAsync(
            nameof(IStaffClient.CandidatePresenceChanged),
            arguments => ((CandidatePresence)arguments[1]!).Connections == 0);
    }

    [Fact]
    public async Task Going_live_keeps_the_candidates_connections()
    {
        // Arrange
        var session = await PlannedSessionAsync();
        var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);
        await using var connection = await ConnectedCandidateAsync(candidate, session.Id);

        // Act
        await session.Staff.WentLiveAsync(session.Id);

        // Assert
        await connection.Inbox.WaitForAsync(nameof(ICandidateClient.SessionStateChanged));
        await connection.Inbox.WaitForAsync(nameof(ICandidateClient.TaskQueueChanged));
        connection.Inbox.IsClosed.ShouldBeFalse();
    }

    private async Task<SignedInStaff> SignInAsColleagueAsync(SignedInStaff staff)
    {
        var colleague = await Host.CreateUserAsync(UserBuilder.Interviewer(staff.User.TeamId!.Value));
        return new SignedInStaff(colleague, await Host.SignInAsync(colleague));
    }

    private async Task<HubClient> SubscribedStaffAsync(SignedInStaff staff, InterviewSessionId sessionId)
    {
        var connection = Host.CreateStaffHubConnection(staff);
        var inbox = HubInbox.OfStaff(connection);
        await connection.StartAsync(CancellationToken);
        await connection.InvokeAsync<CandidatePresence>(SubscribeToSession, sessionId.Value, CancellationToken);
        return new HubClient(connection, inbox);
    }

    private async Task<HubClient> ConnectedCandidateAsync(BrowserSession candidate, InterviewSessionId sessionId)
    {
        var connection = Host.CreateCandidateHubConnection(candidate);
        var inbox = HubInbox.OfCandidate(connection);
        var open = Host.OpenCandidateConnections(sessionId.Value);
        await connection.StartAsync(CancellationToken);
        await Host.CandidateConnectionsOpenedAsync(sessionId.Value, open + 1);
        return new HubClient(connection, inbox);
    }

    private async Task<TestSession> PlannedSessionAsync()
    {
        var interviewer = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = interviewer.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(interviewer);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId));
        var first = await browser.AddedTaskAsync(created.SessionId, await Host.AddBankTaskAsync(teamId));
        var token = await browser.IssuedCandidateLinkAsync(created.Id);
        return new TestSession(interviewer, teamId, browser, null!, created.SessionId, token, first);
    }

    private async Task<TestSession> LiveSessionAsync()
    {
        var session = await PlannedSessionAsync();
        await session.Staff.WentLiveAsync(session.Id);
        var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);
        return session with { Candidate = candidate };
    }

    private sealed record TestSession(
        SignedInStaff Interviewer,
        TeamId TeamId,
        BrowserSession Staff,
        BrowserSession Candidate,
        InterviewSessionId Id,
        string Token,
        Guid First);

    private sealed record HubClient(HubConnection Connection, HubInbox Inbox) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }

    private static class StaffHubMethods
    {
        public const string SubscribeToSession = nameof(SubscribeToSession);
        public const string UnsubscribeFromSession = nameof(UnsubscribeFromSession);
    }
}
