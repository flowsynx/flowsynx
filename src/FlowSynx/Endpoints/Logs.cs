using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Logs.Query.LogsList;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Logs : EndpointGroupBase
{
    /// <summary>
    /// Register the logs endpoints and apply the configured rate-limiting policy.
    /// </summary>
    /// <param name="app">Application builder used to define endpoints.</param>
    /// <param name="rateLimitPolicyName">Named rate-limiting policy applied to this group.</param>
    public override void Map(WebApplication app, string rateLimitPolicyName)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicyName);

        group.MapPost("", LogsList)
            .WithName("LogsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "logs"));
    }

    public static async Task<IResult> LogsList(HttpContext context,
        [FromServices] IMediator mediator, 
        [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<LogsListRequestTdo>(jsonString);

        var result = await mediator.Logs(page, pageSize, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
