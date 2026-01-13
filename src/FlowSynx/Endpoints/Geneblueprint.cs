using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Features.Version.VersionRequest;
using FlowSynx.Application.Models;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Geneblueprint : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        //group.MapPost("", Execute)
        //    .WithName("Execute")
        //    .RequirePermissions(Permissions.Admin);

        group.MapPost("/register", RegisterGene)
            .WithName("RegisterGene")
            .RequirePermissions(Permissions.Admin);
    }

    //public static async Task<IResult> Execute(
    //    [FromBody] object json,
    //    [FromServices] ILogger<Genome> logger,
    //    [FromServices] IGenomeManagementService managementService,
    //    CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        logger.LogInformation("Received execution request");

    //        var jsonString = System.Text.Json.JsonSerializer.Serialize(json);
    //        var result = await managementService.ExecuteJsonAsync(jsonString);

    //        return Results.Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex, "Execution failed");

    //        return Results.Json(new ExecutionResponse
    //        {
    //            Metadata = new ExecutionResponseMetadata
    //            {
    //                Id = Guid.NewGuid().ToString(),
    //                ExecutionId = $"error-{Guid.NewGuid()}",
    //                StartedAt = DateTimeOffset.UtcNow,
    //                CompletedAt = DateTimeOffset.UtcNow
    //            },
    //            Status = new ExecutionStatus
    //            {
    //                Phase = "failed",
    //                Message = ex.Message,
    //                Reason = "InternalError",
    //                Health = "unhealthy"
    //            }
    //        }, statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

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
