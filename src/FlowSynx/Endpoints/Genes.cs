using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
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

        group.MapDelete("/{id:guid}", DeleteGene)
            .WithName("DeleteGene")
            .RequirePermissions(Permissions.Admin, Permissions.Genes);

        group.MapPost("/{id:guid}/execute", ExecuteGene)
            .WithName("ExecuteGene")
            .RequirePermissions(Permissions.Admin, Permissions.Genes);

        group.MapPost("/validate", ValidateGene)
            .WithName("ValidateGene")
            .RequirePermissions(Permissions.Admin, Permissions.Genes);
    }

    public static async Task<IResult> GenesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] string @namespace = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GenesList(page, pageSize, @namespace, cancellationToken);
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
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.RegisterGene(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> DeleteGene(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.DeleteGene(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ExecuteGene(
        [FromRoute] Guid id,
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ExecuteGene(id, json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ValidateGene(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ValidateGene(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
