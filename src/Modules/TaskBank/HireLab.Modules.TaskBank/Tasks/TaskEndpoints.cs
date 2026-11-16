using HireLab.Modules.TaskBank.Tasks.GetTask;
using HireLab.Modules.TaskBank.Tasks.ListTasks;
using HireLab.Modules.TaskBank.Tasks.ListTaskTags;
using HireLab.Modules.TaskBank.Tasks.ListTaskTypes;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.TaskBank.Tasks;

internal static class TaskEndpoints
{
    public const string Tag = "Tasks";

    public static void MapTaskEndpoints(this IEndpointRouteBuilder staff)
    {
        var tasks = staff.MapGroup("/tasks").WithTags(Tag).RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        tasks.MapListTasks();
        tasks.MapListTaskTags();
        tasks.MapListTaskTypes();
        tasks.MapGetTask();
    }
}
