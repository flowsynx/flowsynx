using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Version.Query;
using FlowSynx.Extensions;
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
            .WithOpenApi();
    }

    public async Task<IResult> GetVersion([FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.Version(new VersionRequest(), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}