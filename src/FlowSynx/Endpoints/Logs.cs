using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Logs.Query.LogsList;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Logs : EndpointGroupBase
{
    public override void Map(WebApplication app, string rateLimitPolicy)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicy);

        group.MapPost("", LogsList)
            .WithName("LogsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "logs"));
    }

    public async Task<IResult> LogsList(HttpContext context,
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