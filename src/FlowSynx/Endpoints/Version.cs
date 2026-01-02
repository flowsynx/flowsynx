using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Features.Version.Query;
using FlowSynx.Extensions;
using FlowSynx.Security;
using MediatR;
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
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.Version(new VersionRequest(), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
