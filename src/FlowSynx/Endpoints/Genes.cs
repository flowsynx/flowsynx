using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Genes : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", GenesList)
            .WithName("GeneList")
            .RequirePermissions(Permissions.Admin, Permissions.Genes);

        group.MapGet("/{id:guid}", GeneDetails)
            .WithName("GeneDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Genes);

        group.MapPost("", RegisterGene)
            .WithName("RegisterGene")
            .RequirePermissions(Permissions.Admin, Permissions.Genes);
    }

    public static async Task<IResult> GenesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GenesList(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> GeneDetails(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.GeneDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> RegisterGene(
        [FromBody] object json,
        [FromServices] IGenomeManagementService managementService,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.RegisterGene(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
