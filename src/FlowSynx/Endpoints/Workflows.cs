using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Workflows : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", WorkflowsList)
            .WithName("WorkflowsList")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapGet("/{id:guid}", WorkflowDetails)
            .WithName("WorkflowDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapPost("", CreateWorkflow)
            .WithName("CreateWorkflow")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapDelete("/{id:guid}", DeleteWorkflow)
            .WithName("DeleteWorkflow")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapPost("/{id:guid}/execute", ExecuteWorkflow)
            .WithName("ExecuteWorkflow")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapPost("/validate", ValidateWorkflow)
            .WithName("ValidateWorkflow")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapGet("/{id:guid}/executions", WorkflowExecutionHistoryList)
            .WithName("WorkflowExecutionHistoryList")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapGet("/{id:guid}/activities", WorkflowActivitiesList)
            .WithName("WorkflowActivitiesList")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);
    }

    public static async Task<IResult> WorkflowsList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] string @namespace = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.WorkflowsList(page, pageSize, @namespace, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> WorkflowDetails(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.WorkflowDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> CreateWorkflow(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.CreateWorkflow(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> DeleteWorkflow(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.DeleteWorkflow(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ExecuteWorkflow(
        [FromRoute] Guid id,
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ExecuteWorkflow(id, json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ValidateWorkflow(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ValidateWorkflow(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> WorkflowExecutionHistoryList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.WorkflowExecutionHistoryList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> WorkflowActivitiesList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.WorkflowActivitiesList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}