using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Activities : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", ActivitiesList)
            .WithName("ActivitiesList")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);

        group.MapGet("/{id:guid}", ActivityDetails)
            .WithName("ActivityDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);

        group.MapPost("", CreateActivity)
            .WithName("CreateActivity")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);

        group.MapDelete("/{id:guid}", DeleteActivity)
            .WithName("DeleteActivity")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);

        group.MapPost("/{id:guid}/execute", ExecuteActivity)
            .WithName("ExecuteActivity")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);

        group.MapPost("/validate", ValidateActivity)
            .WithName("ValidateActivity")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);

        group.MapGet("/{id:guid}/executions", ActivityExecutionHistoryList)
            .WithName("ActivityExecutionHistoryList")
            .RequirePermissions(Permissions.Admin, Permissions.Activities);
    }

    public static async Task<IResult> ActivitiesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] string @namespace = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.ActivitiesList(page, pageSize, @namespace, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ActivityDetails(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.ActivityDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> CreateActivity(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.CreateActivity(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> DeleteActivity(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.DeleteActivity(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ExecuteActivity(
        [FromRoute] Guid id,
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ExecuteActivity(id, json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ValidateActivity(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ValidateActivity(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ActivityExecutionHistoryList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.ActivityExecutionHistoryList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
