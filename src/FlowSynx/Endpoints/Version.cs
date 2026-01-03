using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Features.Version.Inquiry;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Version : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", GetVersion)
            .WithName("GetVersion")
            .RequirePermissions(Permissions.Admin);
    }

    public static async Task<IResult> GetVersion(
        [FromServices] IDispatcher dispatcher, 
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Version(new VersionInquiry(), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
