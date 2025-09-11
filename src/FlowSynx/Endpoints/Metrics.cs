using FlowSynx.Application.Extensions;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Metrics : EndpointGroupBase
{
    public override void Map(WebApplication app, string rateLimitPolicy)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicy);

        group.MapGet("", GetWorkflowSummary)
            .WithName("GetWorkflowSummary")
            .WithOpenApi();
    }

    public async Task<IResult> GetWorkflowSummary(
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.GetWorkflowSummary(cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}