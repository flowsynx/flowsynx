using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Chromosomes : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", ChromosomesList)
            .WithName("ChromosomesList")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapPost("", RegisterChromosome)
            .WithName("RegisterChromosome")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);
    }

    public static async Task<IResult> ChromosomesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.ChromosomesList(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
    public static async Task<IResult> RegisterChromosome(
        [FromBody] object json,
        [FromServices] IGenomeManagementService managementService,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.RegisterChromosome(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}