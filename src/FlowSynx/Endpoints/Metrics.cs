using FlowSynx.Application.Extensions;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Metrics : EndpointGroupBase
{
    /// <summary>
    /// Register the metrics endpoints and apply the configured rate-limiting policy.
    /// </summary>
    /// <param name="app">Application builder used to define endpoints.</param>
    /// <param name="rateLimitPolicyName">Named rate-limiting policy applied to this group.</param>
    public override void Map(WebApplication app, string rateLimitPolicyName)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicyName);

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
