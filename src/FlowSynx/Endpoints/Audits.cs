using FlowSynx.Application.Extensions;
using FlowSynx.Application.Serializations;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Audits : EndpointGroupBase
{
    public override void Map(WebApplication app, string rateLimitPolicyName)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicyName);

        group.MapGet("", AuditsList)
            .WithName("AuditsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "audits"));

        group.MapGet("/{id}", AuditDetails)
            .WithName("AuditDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "audits"));
    }

    public static async Task<IResult> AuditsList(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.AuditTrails(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> AuditDetails(string id,
        [FromServices] IMediator mediator, [FromServices] IDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var result = await mediator.AuditDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
