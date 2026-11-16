using HireLab.Modules.Positions.Attachments.DeleteAttachment;
using HireLab.Modules.Positions.Attachments.DownloadAttachment;
using HireLab.Modules.Positions.Attachments.ListAttachments;
using HireLab.Modules.Positions.Attachments.UploadAttachment;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Attachments;

internal static class AttachmentEndpoints
{
    public const string Tag = "Attachments";

    public static void MapAttachmentEndpoints(this IEndpointRouteBuilder staff)
    {
        var attachments = staff.MapGroup("/candidates/{candidateId:guid}/attachments")
            .WithTags(Tag)
            .RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        attachments.MapListAttachments();
        attachments.MapUploadAttachment();
        attachments.MapDownloadAttachment();
        attachments.MapDeleteAttachment();
    }
}
