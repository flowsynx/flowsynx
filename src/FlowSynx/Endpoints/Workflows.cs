using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Workflows.Command.AddWorkflow;
using FlowSynx.Application.Features.Workflows.Command.UpdateWorkflow;
using FlowSynx.Application.Features.WorkflowTriggers.Command.AddWorkflowTrigger;
using FlowSynx.Application.Features.WorkflowTriggers.Command.UpdateWorkflowTrigger;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using FlowSynx.Application.Features.Workflows.Command.GenerateFromIntent;
using FlowSynx.Application.Features.Workflows.Command.OptimizeWorkflow;

namespace FlowSynx.Endpoints;

public class Workflows : EndpointGroupBase
{
    /// <summary>
    /// Register the workflow endpoints and attach the configured rate-limiting policy.
    /// </summary>
    /// <param name="app">Application builder used to wire up the workflow routes.</param>
    /// <param name="rateLimitPolicyName">Named rate-limiting policy applied to the workflow group.</param>
    public override void Map(WebApplication app, string rateLimitPolicyName)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicyName);

        #region workflow
        group.MapGet("", GetAllWorkflows)
            .WithName("GetAllWorkflows")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));

        group.MapPost("", AddWorkflow)
            .WithName("AddWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));

        group.MapGet("/{workflowId}", GetWorkflowById)
            .WithName("GetWorkflowById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));

        group.MapPut("/{workflowId}", UpdateWorkflow)
            .WithName("UpdateWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));

        group.MapDelete("/{workflowId}", DeleteWorkflow)
            .WithName("DeleteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));
        #endregion

        #region Workflow Execution
        group.MapGet("/{workflowId}/executions", GetAllExecutions)
            .WithName("GetAllExecutions")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapPost("/{workflowId}/executions", StartWorkflowExecution)
            .WithName("StartWorkflowExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapGet("/{workflowId}/executions/{executionId}", GetExecutionById)
            .WithName("GetExecutionById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapPost("/{workflowId}/executions/{executionId}/cancel", CancelExecution)
            .WithName("CancelExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapGet("/{workflowId}/executions/{executionId}/logs", GetExecutionLogs)
            .WithName("GetExecutionLogs")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapGet("/{workflowId}/executions/{executionId}/approvals", GetWorkflowExecutionApprovals)
            .WithName("GetWorkflowExecutionApprovals")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "approvals"));

        group.MapPost("/{workflowId}/executions/{executionId}/approvals/{approvalId}/approve", ApproveWorkflowExecution)
            .WithName("ApproveWorkflowExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "approvals"));

        group.MapPost("/{workflowId}/executions/{executionId}/approvals/{approvalId}/reject", RejectWorkflowExecution)
            .WithName("RejectWorkflowExecution")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "approvals"));
        #endregion

        group.MapGet("/{workflowId}/executions/{executionId}/tasks", GetExecutionTasks)
            .WithName("GetExecutionTasks")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapGet("/{workflowId}/executions/{executionId}/tasks/{taskId}", GetTaskExecutionById)
            .WithName("GetTaskExecutionById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapGet("/{workflowId}/executions/{executionId}/tasks/{taskId}/logs", GetTaskExecutionLogs)
            .WithName("GetTaskExecutionLogs")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "executions"));

        group.MapGet("/{workflowId}/triggers", GetAllTriggers)
            .WithName("GetAllTriggers")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "triggers"));

        group.MapPost("/{workflowId}/triggers", AddTrigger)
            .WithName("AddTrigger")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "triggers"));

        group.MapGet("/{workflowId}/triggers/{triggerId}", GetTriggerById)
            .WithName("GetTriggerById")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "triggers"));

        group.MapPut("/{workflowId}/triggers/{triggerId}", UpdateTrigger)
            .WithName("UpdateTrigger")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "triggers"));

        group.MapDelete("/{workflowId}/triggers/{triggerId}", DeleteTrigger)
            .WithName("DeleteTrigger")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "triggers"));

        group.MapPost("/intents", GenerateFromIntent)
            .WithName("GenerateFromIntent")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));

        group.MapPost("/{workflowId}/optimize", OptimizeWorkflow)
            .WithName("OptimizeWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "workflows"));
    }

    public async Task<IResult> GetAllWorkflows(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.Workflows(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddWorkflow(
        HttpContext context,
        [FromServices] IMediator mediator, 
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var (definition, schemaUrl) = ParseWorkflowPayload(jsonString, jsonDeserializer);
        var request = new AddWorkflowRequest
        {
            SchemaUrl = schemaUrl,
            Definition = definition
        };

        var result = await mediator.AddWorkflow(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetWorkflowById(
        string workflowId, 
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowDetails(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdateWorkflow(
        string workflowId, 
        HttpContext context,
        [FromServices] IMediator mediator, 
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var (definition, schemaUrl) = ParseWorkflowPayload(jsonString, jsonDeserializer);
        var request = new UpdateWorkflowRequest
        {
            WorkflowId = workflowId,
            Definition = definition,
            SchemaUrl = schemaUrl
        };

        var result = await mediator.UpdateWorkflow(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteWorkflow(
        string workflowId, 
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteWorkflow(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    private static (string Definition, string? SchemaUrl) ParseWorkflowPayload(
        string jsonBody,
        IJsonDeserializer jsonDeserializer)
    {
        if (string.IsNullOrWhiteSpace(jsonBody))
            return (string.Empty, null);

        try
        {
            var payload = jsonDeserializer.Deserialize<WorkflowUpsertPayload>(jsonBody);

            if (!payload.HasEnvelopeFields)
                return (jsonBody, null);

            var definition = payload.Workflow switch
            {
                string definitionString => definitionString,
                null => string.Empty,
                _ => payload.Workflow.ToString() ?? string.Empty
            };

            return (definition, payload.Schema);
        }
        catch (FlowSynx.PluginCore.Exceptions.FlowSynxException)
        {
            throw;
        }
        catch
        {
            return (jsonBody, null);
        }
    }

    public async Task<IResult> GetAllExecutions(
        string workflowId, 
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.GetWorkflowExecutionsList(workflowId, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> StartWorkflowExecution(
        string workflowId, 
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.ExecuteWorkflow(workflowId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetExecutionById(
        string workflowId, 
        string executionId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowExecutionDetails(workflowId, executionId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetExecutionTasks(
        string workflowId, 
        string executionId, 
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.WorkflowExecutionTasks(workflowId, executionId, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetTaskExecutionById(
        string workflowId, 
        string executionId, 
        string taskId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTaskExecutionDetails(workflowId, executionId, taskId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> CancelExecution(
        string workflowId, 
        string executionId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.CancelWorkflowExecution(workflowId, executionId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetExecutionLogs(
        string workflowId, 
        string executionId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.WorkflowExecutionLogs(workflowId, executionId, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetWorkflowExecutionApprovals(
        string workflowId, 
        string executionId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.GetWorkflowPendingApprovals(workflowId, executionId, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> ApproveWorkflowExecution(
        string workflowId, 
        string executionId,
        string approvalId, 
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.ApproveWorkflowExecution(workflowId, executionId, approvalId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> RejectWorkflowExecution(
        string workflowId, 
        string executionId,
        string approvalId, 
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.RejectWorkflowExecution(workflowId, executionId, approvalId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetTaskExecutionLogs(
        string workflowId, 
        string executionId, 
        string taskId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.WorkflowTaskExecutionLogs(workflowId, executionId, taskId, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetAllTriggers(
        string workflowId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.WorkflowTriggersList(workflowId, page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetTriggerById(
        string workflowId, 
        string triggerId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowTriggerDetails(workflowId, triggerId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddTrigger(
        HttpContext context, 
        string workflowId,
        [FromServices] IMediator mediator, 
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<AddWorkflowTriggerDefinition>(jsonString);

        var result = await mediator.AddWorkflowTrigger(workflowId, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdateTrigger(
        HttpContext context, 
        string workflowId,
        string triggerId, 
        [FromServices] IMediator mediator,
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UpdateWorkflowTriggerDefinition>(jsonString);

        var result = await mediator.UpdateWorkflowTrigger(workflowId, triggerId, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteTrigger(
        string workflowId, 
        string triggerId,
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteWorkflowTrigger(workflowId, triggerId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GenerateFromIntent(
        HttpContext context,
        [FromServices] IMediator mediator,
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<GenerateFromIntentRequest>(jsonString);

        var result = await mediator.GenerateWorkflowFromIntent(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
    }

    public async Task<IResult> OptimizeWorkflow(
        string workflowId,
        HttpContext context,
        [FromServices] IMediator mediator,
        [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var input = string.IsNullOrWhiteSpace(jsonString) ? new { ApplyChanges = false, SchemaUrl = (string?)null }
                                                          : jsonDeserializer.Deserialize<dynamic>(jsonString);

        var request = new OptimizeWorkflowRequest
        {
            WorkflowId = workflowId,
            ApplyChanges = (bool?)input?.ApplyChanges ?? false,
            SchemaUrl = (string?)input?.SchemaUrl
        };

        var result = await mediator.OptimizeWorkflow(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
    }

    private sealed class WorkflowUpsertPayload
    {
        public string? Schema { get; init; }
        public object? Workflow { get; init; }
        public bool HasEnvelopeFields => Workflow is not null || !string.IsNullOrWhiteSpace(Schema);
    }
}
