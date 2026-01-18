using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
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

        group.MapGet("/{id:guid}", ChromosomeDetails)
            .WithName("ChromosomeDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapPost("", CreateChromosome)
            .WithName("CreateChromosome")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapDelete("/{id:guid}", DeleteChromosome)
            .WithName("DeleteChromosome")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapPost("/{id:guid}/execute", ExecuteChromosome)
            .WithName("ExecuteChromosome")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapPost("/validate", ValidateChromosome)
            .WithName("ValidateChromosome")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapGet("/{id:guid}/executions", ChromosomeExecutionHistoryList)
            .WithName("ChromosomeExecutionHistoryList")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);

        group.MapGet("/{id:guid}/genes", ChromosomeGenesList)
            .WithName("ChromosomeGenesList")
            .RequirePermissions(Permissions.Admin, Permissions.Chromosomes);
    }

    public static async Task<IResult> ChromosomesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] string @namespace = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.ChromosomesList(page, pageSize, @namespace, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ChromosomeDetails(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.ChromosomeDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> CreateChromosome(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.CreateChromosome(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> DeleteChromosome(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.DeleteChromosome(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ExecuteChromosome(
        [FromRoute] Guid id,
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ExecuteChromosome(id, json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ValidateChromosome(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ValidateChromosome(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ChromosomeExecutionHistoryList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.ChromosomeExecutionHistoryList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ChromosomeGenesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.ChromosomeGenesList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}