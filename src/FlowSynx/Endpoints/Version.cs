using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Version.Query;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Version : EndpointGroupBase
{
    public override void Map(WebApplication app, string rateLimitPolicy)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicy);

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