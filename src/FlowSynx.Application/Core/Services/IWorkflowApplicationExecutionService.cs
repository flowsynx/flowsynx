using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowExecutions;

namespace FlowSynx.Application.Core.Services;

public interface IWorkflowApplicationExecutionService
{
    Task<Result<ExecutionResponse>> ExecuteActivityAsync(
        TenantId tenantId,
        string userId,
        Guid activityId, 
        Dictionary<string, object> parameters, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<Result<ExecutionResponse>> ExecuteWorkflowAsync(
        TenantId tenantId,
        string userId,
        Guid workflowId, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default);

    Task<Result<ExecutionResponse>> ExecuteWorkflowApplicationAsync(
        TenantId tenantId,
        string userId,
        Guid workflowApplicationId, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<Result<ExecutionResponse>> ExecuteRequestAsync(
        TenantId tenantId,
        string userId,
        ExecutionRequest request, 
        CancellationToken cancellationToken = default);

    Task<WorkflowExecution?> GetWorkflowExecutionAsync(
        Guid executionId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowExecution>> GetExecutionHistoryAsync(
        string targetType, 
        Guid targetId,
        CancellationToken cancellationToken = default);
}