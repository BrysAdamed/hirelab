using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using HireLab.Testing.TaskTypes;
using Microsoft.AspNetCore.TestHost;

namespace HireLab.Api.Tests.TaskTypes;

public sealed class TaskTypeStartupTests(HireLabApiFactory factory) : IClassFixture<HireLabApiFactory>
{
    [Fact]
    public void Api_fails_to_start_when_two_task_types_share_a_key()
    {
        // Arrange
        using var duplicated = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddTaskType<TestEchoTaskType>().AddTaskType<ImpostorEchoTaskType>()));

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => duplicated.Services);

        // Assert
        exception.Message.ShouldContain($"'{TestEchoTaskType.TaskTypeKey}'");
    }

    private sealed class ImpostorEchoTaskType : TaskType<TestEchoCandidateContent, TestEchoStaffContent, TestEchoResponse>
    {
        public override string Key => TestEchoTaskType.TaskTypeKey;

        public override int ContentSchemaVersion => 1;

        public override int ResponseSchemaVersion => 1;

        public override Result ValidateContent(TaskContent<TestEchoCandidateContent, TestEchoStaffContent> content) => Result.Success();

        public override Result ValidateResponse(TestEchoResponse response) => Result.Success();
    }
}
