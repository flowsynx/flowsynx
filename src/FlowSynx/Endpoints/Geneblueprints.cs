using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Geneblueprints : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", GeneblueprintsList)
            .WithName("GeneblueprintsList")
            .RequirePermissions(Permissions.Admin, Permissions.Geneblueprints);

        group.MapPost("", RegisterGene)
            .WithName("RegisterGene")
            .RequirePermissions(Permissions.Admin);
    }

    public static async Task<IResult> GeneblueprintsList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GeneBlueprintsList(page, pageSize, cancellationToken);
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
