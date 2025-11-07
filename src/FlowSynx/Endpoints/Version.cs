using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Version.Query;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Version : EndpointGroupBase
{
    /// <summary>
    /// Register the version endpoint group and apply the configured rate-limiting policy.
    /// </summary>
    /// <param name="app">Application builder used to define endpoints.</param>
    /// <param name="rateLimitPolicyName">Named rate-limiting policy applied to this group.</param>
    public override void Map(WebApplication app, string rateLimitPolicyName)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicyName);

        group.MapGet("", GetVersion)
            .WithName("GetVersion")
            .WithOpenApi();
    }

    /// <summary>
    /// Retrieve the current application version metadata via the mediator pipeline.
    /// </summary>
    /// <param name="mediator">Mediator instance responsible for executing the version request.</param>
    /// <param name="cancellationToken">Cancellation token propagated from the HTTP request.</param>
    public async Task<IResult> GetVersion([FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.Version(new VersionRequest(), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
