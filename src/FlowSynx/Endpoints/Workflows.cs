using FlowSynx.Application.Extensions;
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

        group.MapGet("", WorkflowsList)
            .WithName("WorkflowsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{id}", WorkflowDetails)
            .WithName("WorkflowDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("", AddWorkflow)
            .WithName("AddWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPut("/{id}", UpdateWorkflow)
            .WithName("UpdateWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapDelete("/{id}", DeleteWorkflow)
            .WithName("DeleteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/{id}/executions", ExecuteWorkflow)
            .WithName("ExecuteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{id}/executions/{execId}", WorkflowExecutionDetails)
            .WithName("WorkflowExecutionDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{id}/executions/{execId}/tasks/{taskId}", WorkflowTaskExecutionDetails)
            .WithName("WorkflowTaskExecutionDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{id}/executions/{execId}/cancel", CancelWorkflowExecution)
            .WithName("CancelWorkflowExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{id}/executions/{execId}/logs", WorkflowExecutionLogs)
            .WithName("WorkflowExecutionLogs")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/{id}/executions/{execId}/logs/{taskId}", WorkflowTaskExecutionLogs)
            .WithName("WorkflowTaskExecutionLogs")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));
    }

    public async Task<IResult> WorkflowsList([FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Workflows(cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowDetails(string id, [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowDetails(id, cancellationToken);
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

    public async Task<IResult> UpdateWorkflow(string id, HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var result = await mediator.UpdateWorkflow(id, jsonString, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteWorkflow(string id, [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteWorkflow(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> ExecuteWorkflow(Guid id, [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.ExecuteWorkflow(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowExecutionDetails(string id, string execId, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowExecutionDetails(id, execId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowTaskExecutionDetails(string id, string execId, string taskId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTaskExecutionDetails(id, execId, taskId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> CancelWorkflowExecution(string id, string execId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.CancelWorkflowExecution(id, execId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowExecutionLogs(string id, string execId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowExecutionLogs(id, execId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowTaskExecutionLogs(string id, string execId, string taskId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTaskExecutionLogs(id, execId, taskId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}