using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Features.Activities.Actions.CreateActivity;
using FlowSynx.Application.Features.Activities.Actions.DeleteActivity;
using FlowSynx.Application.Features.Activities.Actions.ExecuteActivity;
using FlowSynx.Application.Features.Activities.Actions.ValidateActivity;
using FlowSynx.Application.Features.Activities.Requests.ActivitiesList;
using FlowSynx.Application.Features.Activities.Requests.ActivityDetails;
using FlowSynx.Application.Features.Activities.Requests.ActivityExecutionsList;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;
using FlowSynx.Application.Features.Execute;
using FlowSynx.Application.Features.Tenants.Actions.AddTenant;
using FlowSynx.Application.Features.Tenants.Actions.DeleteTenant;
using FlowSynx.Application.Features.Tenants.Actions.UpdateTenant;
using FlowSynx.Application.Features.Tenants.Requests.TenantsDetails;
using FlowSynx.Application.Features.Tenants.Requests.TenantsList;
using FlowSynx.Application.Features.Version.VersionRequest;
using FlowSynx.Application.Features.WorkflowApplications.Actions.CreateWorkflowApplication;
using FlowSynx.Application.Features.WorkflowApplications.Actions.DeleteWorkflowApplication;
using FlowSynx.Application.Features.WorkflowApplications.Actions.ExecuteWorkflowApplication;
using FlowSynx.Application.Features.WorkflowApplications.Actions.ValidateWorkflowApplication;
using FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationDetails;
using FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationExecutionsList;
using FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationsList;
using FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationWorkflowsList;
using FlowSynx.Application.Features.Workflows.Actions.CreateWorkflow;
using FlowSynx.Application.Features.Workflows.Actions.DeleteWorkflow;
using FlowSynx.Application.Features.Workflows.Actions.ExecuteWorkflow;
using FlowSynx.Application.Features.Workflows.Actions.ValidateWorkflow;
using FlowSynx.Application.Features.Workflows.Requests.WorkflowActivitiesList;
using FlowSynx.Application.Features.Workflows.Requests.WorkflowDetails;
using FlowSynx.Application.Features.Workflows.Requests.WorkflowExecutionsList;
using FlowSynx.Application.Features.Workflows.Requests.WorkflowsList;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Core.Extensions;

public static class DispatcherExtensions
{
    #region AuditTrails
    public static Task<PaginatedResult<AuditTrailsListResult>> AuditTrails(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new AuditTrailsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<AuditTrailDetailsResult>> AuditDetails(
        this IDispatcher dispatcher,
        long auditId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new AuditTrailDetailsRequest { AuditId = auditId }, cancellationToken);
    }
    #endregion

    #region Execute
    public static Task<Result<ExecutionResponse>> Execute(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteRequest { Json = json }, cancellationToken);
    }
    #endregion

    #region Activities
    public static Task<PaginatedResult<ActivitiesListResult>> ActivitiesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ActivitiesListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<ActivityDetailsResult>> ActivityDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ActivityDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<CreateActivityResult>> CreateActivity(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new CreateActivityRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteActivity(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteActivityRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteActivity(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteActivityRequest
        {
            ActivityId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateActivity(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateActivityRequest
        {
            Json = json
        }, cancellationToken);
    }

    public static Task<PaginatedResult<ActivityExecutionsListResult>> ActivityExecutionHistoryList(
        this IDispatcher dispatcher,
        Guid activityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ActivityExecutionsListRequest
        {
            ActivityId = activityId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }
    #endregion

    #region WorkflowApplications
    public static Task<PaginatedResult<WorkflowApplicationsListResult>> WorkflowApplicationsList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowApplicationsListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<WorkflowApplicationDetailsResult>> WorkflowApplicationDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowApplicationDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<CreateWorkflowApplicationResult>> CreateWorkflowApplication(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new CreateWorkflowApplicationRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteWorkflowApplication(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteWorkflowApplicationRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteWorkflowApplication(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteWorkflowApplicationRequest
        {
            WorkflowApplicationId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateWorkflowApplication(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateWorkflowApplicationRequest
        {
            Json = json
        }, cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowApplicationExecutionsListResult>> WorkflowApplicationExecutionHistoryList(
        this IDispatcher dispatcher,
        Guid workflowApplicationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowApplicationExecutionsListRequest
        {
            WorkflowApplicationId = workflowApplicationId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowApplicationWorkflowsListResult>> WorkflowApplicationWorkflowsList(
        this IDispatcher dispatcher,
        Guid workflowApplicationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowApplicationWorkflowsListRequest
        {
            WorkflowApplicationId = workflowApplicationId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }
    #endregion

    #region Workflows
    public static Task<PaginatedResult<WorkflowsListResult>> WorkflowsList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowsListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<WorkflowDetailsResult>> WorkflowDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<CreateWorkflowResult>> CreateWorkflow(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new CreateWorkflowRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteWorkflow(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteWorkflowRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteWorkflow(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteWorkflowRequest
        {
            WorkflowId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateWorkflow(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateWorkflowRequest
        {
            Json = json
        }, cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowExecutionsListResult>> WorkflowExecutionHistoryList(
        this IDispatcher dispatcher,
        Guid workflowId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowExecutionsListRequest
        {
            WorkflowId = workflowId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowActivitiesListResult>> WorkflowActivitiesList(
        this IDispatcher dispatcher,
        Guid workflowId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new WorkflowActivitiesListRequest
        {
            WorkflowId = workflowId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }
    #endregion

    #region Version
    public static Task<Result<VersionResult>> Version(
        this IDispatcher dispatcher,
        VersionRequest request,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(request, cancellationToken);
    }
    #endregion

    #region Tenants
    public static Task<PaginatedResult<TenantsListResult>> Tenants(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new TenantsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<TenantDetailsResult>> TenantDetails(
        this IDispatcher dispatcher,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new TenantDetailsRequest { TenantId = tenantId }, cancellationToken);
    }

    public static Task<Result<AddTenantResult>> AddTenant(
        this IDispatcher dispatcher,
        AddTenantRequest tenantRequest,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(tenantRequest, cancellationToken);
    }

    public static Task<Result<Void>> UpdateTenant(
        this IDispatcher dispatcher,
        Guid tenantId,
        UpdateTenantDefinitionRequest updateTenantRequest,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new UpdateTenantRequest
        {
            TenantId = tenantId,
            Name = updateTenantRequest.Name,
            Description = updateTenantRequest.Description,
            Status = updateTenantRequest.Status
        }, cancellationToken);
    }

    public static Task<Result<DeleteTenantResult>> DeleteTenant(
        this IDispatcher dispatcher,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteTenantRequest { tenantId = tenantId }, cancellationToken);
    }
    #endregion
}