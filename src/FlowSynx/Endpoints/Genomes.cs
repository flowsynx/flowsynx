using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Genomes : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", GenomesList)
            .WithName("GenomesList")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapGet("/{id:guid}", GenomeDetails)
            .WithName("GenomeDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapPost("", CreateGenome)
            .WithName("CreateGenome")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapDelete("/{id:guid}", DeleteGenome)
            .WithName("DeleteGenome")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapPost("/{id:guid}/execute", ExecuteGenome)
            .WithName("ExecuteGenome")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapPost("/validate", ValidateGenome)
            .WithName("ValidateGenome")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapGet("/{id:guid}/executions", GenomeExecutionHistoryList)
            .WithName("GenomeExecutionHistoryList")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);

        group.MapGet("/{id:guid}/chromosomes", GenomeChromosomesList)
            .WithName("GenomeChromosomesList")
            .RequirePermissions(Permissions.Admin, Permissions.Genomes);
    }

    public static async Task<IResult> GenomesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] string @namespace = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GenomesList(page, pageSize, @namespace, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> GenomeDetails(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.GenomeDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> CreateGenome(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.CreateGenome(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> DeleteGenome(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.DeleteGenome(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ExecuteGenome(
        [FromRoute] Guid id,
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ExecuteGenome(id, json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ValidateGenome(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ValidateGenome(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> GenomeExecutionHistoryList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GenomeExecutionHistoryList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> GenomeChromosomesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GenomeChromosomesList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}