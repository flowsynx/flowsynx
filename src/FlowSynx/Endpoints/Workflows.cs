using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Workflows.Command.AddWorkflowTrigger;
using FlowSynx.Application.Features.Workflows.Command.UpdateWorkflowTrigger;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Workflows : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        #region workflow
        group.MapGet("", GetAllWorkflows)
            .WithName("GetAllWorkflows")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("", AddWorkflow)
            .WithName("AddWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{workflowId}", GetWorkflowById)
            .WithName("GetWorkflowById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPut("/{workflowId}", UpdateWorkflow)
            .WithName("UpdateWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapDelete("/{workflowId}", DeleteWorkflow)
            .WithName("DeleteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));
        #endregion

        #region Workflow Execution
        group.MapGet("/{workflowId}/executions", GetAllExecutions)
            .WithName("GetAllExecutions")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/{workflowId}/executions", StartWorkflowExecution)
            .WithName("StartWorkflowExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{workflowId}/executions/{executionId}", GetExecutionById)
            .WithName("GetExecutionById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/{workflowId}/executions/{executionId}/cancel", CancelExecution)
            .WithName("CancelExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{workflowId}/executions/{executionId}/logs", GetExecutionLogs)
            .WithName("GetExecutionLogs")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));
        #endregion

        group.MapGet("/{workflowId}/executions/{executionId}/tasks/{taskId}", GetTaskExecutionById)
            .WithName("GetTaskExecutionById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{workflowId}/executions/{executionId}/tasks/{taskId}/logs", GetTaskExecutionLogs)
            .WithName("GetTaskExecutionLogs")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{workflowId}/triggers", GetAllTriggers)
            .WithName("GetAllTriggers")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/{workflowId}/triggers", AddTrigger)
            .WithName("AddTrigger")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{workflowId}/triggers/{triggerId}", GetTriggerById)
            .WithName("GetTriggerById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPut("/{workflowId}/triggers/{triggerId}", UpdateTrigger)
            .WithName("UpdateTrigger")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapDelete("/{workflowId}/triggers/{triggerId}", DeleteTrigger)
            .WithName("DeleteTrigger")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));
    }

    public async Task<IResult> GetAllWorkflows([FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Workflows(cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddWorkflow(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var result = await mediator.AddWorkflow(jsonString, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetWorkflowById(string workflowId, [FromServices] IMediator mediator,
    CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowDetails(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdateWorkflow(string workflowId, HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var result = await mediator.UpdateWorkflow(workflowId, jsonString, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteWorkflow(string workflowId, [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteWorkflow(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetAllExecutions(string workflowId, [FromServices] IMediator mediator,
    CancellationToken cancellationToken)
    {
        var result = await mediator.GetWorkflowExecutionsList(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> StartWorkflowExecution(string workflowId, [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.ExecuteWorkflow(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetExecutionById(string workflowId, string executionId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowExecutionDetails(workflowId, executionId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetTaskExecutionById(string workflowId, string executionId, string taskId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTaskExecutionDetails(workflowId, executionId, taskId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> CancelExecution(string workflowId, string executionId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.CancelWorkflowExecution(workflowId, executionId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetExecutionLogs(string workflowId, string executionId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowExecutionLogs(workflowId, executionId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetTaskExecutionLogs(string workflowId, string executionId, string taskId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTaskExecutionLogs(workflowId, executionId, taskId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetAllTriggers(string workflowId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTriggersList(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetTriggerById(string workflowId, string triggerId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTriggerDetails(workflowId, triggerId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddTrigger(HttpContext context, string workflowId,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<AddWorkflowTriggerDefinition>(jsonString);

        var result = await mediator.AddWorkflowTrigger(workflowId, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdateTrigger(HttpContext context, string workflowId,
        string triggerId, [FromServices] IMediator mediator, 
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UpdateWorkflowTriggerDefinition>(jsonString);

        var result = await mediator.UpdateWorkflowTrigger(workflowId, triggerId, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteTrigger(string workflowId, string triggerId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteWorkflowTrigger(workflowId, triggerId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}