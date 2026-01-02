using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Audits : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", AuditsList)
            .WithName("AuditsList")
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "audits"));

        group.MapGet("/{id:long}", AuditDetails)
            .WithName("AuditDetails")
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

    public static async Task<IResult> AuditDetails(long id,
        [FromServices] IMediator mediator, [FromServices] IDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var result = await mediator.AuditDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
