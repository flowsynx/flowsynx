using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Application.Core.Services;

public interface IWorkflowApplicationManagementService
{
    Task<Activity> RegisterActivityAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<Workflow> RegisterWorkflowAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<WorkflowApplication> RegisterWorkflowApplicationAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<ValidationResponse> ValidateJsonAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Activity>> SearchActivitiesAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Workflow>> GetWorkflowsByApplicationIdAsync(
        string userId, 
        Guid workflowApplicationId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowApplication>> GetWorkflowApplicationsByOwnerAsync(
        string userId, 
        string owner, 
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteJsonAsync(
        TenantId tenantId,
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> GetExecutionResultAsync(
        string userId, 
        Guid executionId, 
        CancellationToken cancellationToken = default);
}