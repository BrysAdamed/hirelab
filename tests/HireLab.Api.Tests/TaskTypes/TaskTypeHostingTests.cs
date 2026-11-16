using System.Net;
using System.Net.Http.Json;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions;
using HireLab.Testing.TaskTypes;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Api.Tests.TaskTypes;

public sealed class TaskTypeHostingTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly Uri _echoUri = new($"{TestEchoTaskType.EchoPath}?text=hello", UriKind.Relative);

    [Fact]
    public void Registry_holds_the_test_echo_task_type()
    {
        // Arrange
        var registry = Host.Services.GetRequiredService<ITaskTypeRegistry>();

        // Act
        var taskType = registry.Find(TestEchoTaskType.TaskTypeKey);

        // Assert
        taskType.ShouldBeOfType<TestEchoTaskType>();
    }

    [Fact]
    public async Task Task_type_endpoint_answers_the_candidate()
    {
        // Arrange
        using var client = Host.CreateClient(await Host.SignInAsCandidateAsync());

        // Act
        using var response = await client.GetAsync(_echoUri, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TestEchoReply>(CancellationToken)).ShouldBe(new TestEchoReply("hello"));
    }

    [Fact]
    public async Task Task_type_endpoint_rejects_anonymous_requests()
    {
        // Arrange
        using var client = Host.CreateClient();

        // Act
        using var response = await client.GetAsync(_echoUri, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Task_type_endpoint_rejects_the_staff_cookie()
    {
        // Arrange
        using var client = CreateClient(await SignInAsAsync(StaffRole.Interviewer));

        // Act
        using var response = await client.GetAsync(_echoUri, CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
