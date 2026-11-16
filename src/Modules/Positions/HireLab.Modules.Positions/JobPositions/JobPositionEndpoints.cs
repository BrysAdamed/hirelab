using HireLab.Modules.Positions.JobPositions.CloseJobPosition;
using HireLab.Modules.Positions.JobPositions.CreateJobPosition;
using HireLab.Modules.Positions.JobPositions.DeleteJobPosition;
using HireLab.Modules.Positions.JobPositions.EditJobPosition;
using HireLab.Modules.Positions.JobPositions.GetJobPosition;
using HireLab.Modules.Positions.JobPositions.ListJobPositions;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions;

internal static class JobPositionEndpoints
{
    public const string Tag = "JobPositions";

    public static void MapJobPositionEndpoints(this IEndpointRouteBuilder staff)
    {
        var positions = staff.MapGroup("/positions").WithTags(Tag).RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        positions.MapListJobPositions();
        positions.MapGetJobPosition();

        var managed = positions.MapGroup(string.Empty).RequireAuthorization(StaffPolicies.Manager);
        managed.MapCreateJobPosition();
        managed.MapEditJobPosition();
        managed.MapDeleteJobPosition();
        managed.MapCloseJobPosition();
    }
}
