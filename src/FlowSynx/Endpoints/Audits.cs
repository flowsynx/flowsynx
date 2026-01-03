using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Audits : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", AuditsList)
            .WithName("AuditsList")
            .RequirePermissions(Permissions.Admin, Permissions.Audits);

        group.MapGet("/{id:long}", AuditDetails)
            .WithName("AuditDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Audits);
    }

    public static async Task<IResult> AuditsList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.AuditTrails(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> AuditDetails(long id,
        [FromServices] IDispatcher dispatcher, [FromServices] IDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.AuditDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
