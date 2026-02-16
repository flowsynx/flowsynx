using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class WorkflowApplications : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", WorkflowApplicationsList)
            .WithName("WorkflowApplicationsList")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapGet("/{id:guid}", WorkflowApplicationDetails)
            .WithName("WorkflowApplicationDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapPost("", CreateWorkflowApplication)
            .WithName("CreateWorkflowApplication")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapDelete("/{id:guid}", DeleteWorkflowApplication)
            .WithName("DeleteWorkflowApplication")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapPost("/{id:guid}/execute", ExecuteWorkflowApplication)
            .WithName("ExecuteWorkflowApplication")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapPost("/validate", ValidateWorkflowApplication)
            .WithName("ValidateWorkflowApplication")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapGet("/{id:guid}/executions", WorkflowApplicationExecutionHistoryList)
            .WithName("WorkflowApplicationExecutionHistoryList")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);

        group.MapGet("/{id:guid}/activities", WorkflowApplicationWorkflowsList)
            .WithName("WorkflowApplicationWorkflowsList")
            .RequirePermissions(Permissions.Admin, Permissions.Workflows);
    }

    public static async Task<IResult> WorkflowApplicationsList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] string @namespace = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.WorkflowApplicationsList(page, pageSize, @namespace, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> WorkflowApplicationDetails(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.WorkflowApplicationDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> CreateWorkflowApplication(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.CreateWorkflowApplication(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> DeleteWorkflowApplication(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id)
    {
        var result = await dispatcher.DeleteWorkflowApplication(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ExecuteWorkflowApplication(
        [FromRoute] Guid id,
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ExecuteWorkflowApplication(id, json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> ValidateWorkflowApplication(
        [FromBody] object json,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.ValidateWorkflowApplication(json, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> WorkflowApplicationExecutionHistoryList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.WorkflowApplicationExecutionHistoryList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> WorkflowApplicationWorkflowsList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.WorkflowApplicationWorkflowsList(id, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}