using MediatR;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Features.Audit.Query.AuditDetails;
using FlowSynx.Application.Features.Audit.Query.AuditsList;
using FlowSynx.Application.Features.Logs.Query.LogsList;
using FlowSynx.Application.Features.PluginConfig.Command.AddPluginConfig;
using FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;
using FlowSynx.Application.Features.PluginConfig.Command.UpdatePluginConfig;
using FlowSynx.Application.Features.PluginConfig.Query.PluginConfigDetails;
using FlowSynx.Application.Features.PluginConfig.Query.PluginConfigList;
using FlowSynx.Application.Features.Plugins.Command.UninstallPlugin;
using FlowSynx.Application.Features.Plugins.Command.InstallPlugin;
using FlowSynx.Application.Features.Plugins.Command.UpdatePlugin;
using FlowSynx.Application.Features.Plugins.Query.PluginDetails;
using FlowSynx.Application.Features.Plugins.Query.PluginsList;
using FlowSynx.Application.Features.Version.Query;
using FlowSynx.Application.Features.Workflows.Command.AddWorkflow;
using FlowSynx.Application.Features.Workflows.Command.DeleteWorkflow;
using FlowSynx.Application.Features.Workflows.Command.UpdateWorkflow;
using FlowSynx.Application.Features.Workflows.Query.WorkflowDetails;
using FlowSynx.Application.Features.Workflows.Query.WorkflowsList;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionDetails;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;
using FlowSynx.Application.Features.WorkflowExecutions.Command.CancelWorkflow;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;
using FlowSynx.Application.Features.WorkflowTriggers.Query.WorkflowTriggersList;
using FlowSynx.Application.Features.WorkflowTriggers.Query.WorkflowTriggerDetails;
using FlowSynx.Application.Features.WorkflowTriggers.Command.DeleteWorkflowTrigger;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Features.WorkflowTriggers.Command.AddWorkflowTrigger;
using FlowSynx.Application.Features.WorkflowTriggers.Command.UpdateWorkflowTrigger;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionApprovals;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ApproveWorkflow;
using FlowSynx.Application.Features.WorkflowExecutions.Command.RejectWorkflow;
using FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionTasks;
using FlowSynx.Application.Features.Metrics.Query;

namespace FlowSynx.Application.Extensions;

public static class MediatorExtensions
{
    #region Workflow
    public static Task<PaginatedResult<WorkflowListResponse>> Workflows(
        this IMediator mediator,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<WorkflowDetailsResponse>> WorkflowDetails(
        this IMediator mediator,
        string workflowId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowDetailsRequest { WorkflowId = workflowId }, cancellationToken);
    }

    public static Task<Result<AddWorkflowResponse>> AddWorkflow(
        this IMediator mediator,
        AddWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UpdateWorkflow(
        this IMediator mediator,
        UpdateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> DeleteWorkflow(
        this IMediator mediator,
        string workflowId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeleteWorkflowRequest { WorkflowId = workflowId }, cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowExecutionListResponse>> GetWorkflowExecutionsList(
        this IMediator mediator,
        string workflowId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowExecutionListRequest
        {
            WorkflowId = workflowId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<ExecuteWorkflowResponse>> ExecuteWorkflow(
        this IMediator mediator,
        string workflowId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new ExecuteWorkflowRequest { WorkflowId = workflowId }, cancellationToken);
    }

    public static Task<Result<WorkflowExecutionDetailsResponse>> WorkflowExecutionDetails(
        this IMediator mediator,
        string id,
        string executionId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowExecutionDetailsRequest { WorkflowId = id, WorkflowExecutionId = executionId },
            cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowExecutionTasksResponse>> WorkflowExecutionTasks(
        this IMediator mediator,
        string workflowId,
        string executionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowExecutionTasksRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            Page = page,
            PageSize = pageSize
        },
        cancellationToken);
    }

    public static Task<Result<WorkflowTaskExecutionDetailsResponse>> WorkflowTaskExecutionDetails(
        this IMediator mediator,
        string workflowId,
        string executionId,
        string taskId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowTaskExecutionDetailsRequest {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            WorkflowTaskExecutionId = taskId
        },
        cancellationToken);
    }

    public static Task<Result<Unit>> CancelWorkflowExecution(
        this IMediator mediator,
        string workflowId,
        string executionId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new CancelWorkflowRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId
        },
        cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowExecutionLogsResponse>> WorkflowExecutionLogs(
        this IMediator mediator,
        string workflowId,
        string executionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowExecutionLogsRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            Page = page,
            PageSize = pageSize
        },
        cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowTaskExecutionLogsResponse>> WorkflowTaskExecutionLogs(
        this IMediator mediator,
        string workflowId,
        string executionId,
        string taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowTaskExecutionLogsRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            WorkflowTaskExecutionId = taskId,
            Page = page,
            PageSize = pageSize
        },
        cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowTriggersListResponse>> WorkflowTriggersList(
        this IMediator mediator,
        string workflowId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowTriggersListRequest
        {
            WorkflowId = workflowId,
            Page = page,
            PageSize = pageSize
        },
        cancellationToken);
    }

    public static Task<Result<WorkflowTriggerDetailsResponse>> WorkflowTriggerDetails(
        this IMediator mediator,
        string workflowId,
        string triggerId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowTriggerDetailsRequest
        {
            WorkflowId = workflowId,
            TriggerId = triggerId
        },
        cancellationToken);
    }

    public static Task<Result<AddWorkflowTriggerResponse>> AddWorkflowTrigger(
        this IMediator mediator,
        string workflowId,
        AddWorkflowTriggerDefinition request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new AddWorkflowTriggerRequest
        {
            WorkflowId = workflowId,
            Status = request.Status,
            Type = request.Type,
            Properties = request.Properties,
        }, cancellationToken);
    }

    public static Task<Result<Unit>> UpdateWorkflowTrigger(
        this IMediator mediator,
        string workflowId,
        string triggerId,
        UpdateWorkflowTriggerDefinition request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new UpdateWorkflowTriggerRequest
        {
            WorkflowId = workflowId,
            TriggerId = triggerId,
            Status = request.Status,
            Type = request.Type,
            Properties = request.Properties,
        }, cancellationToken);
    }

    public static Task<Result<Unit>> DeleteWorkflowTrigger(
        this IMediator mediator,
        string workflowId,
        string triggerId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeleteWorkflowTriggerRequest
        {
            WorkflowId = workflowId,
            TriggerId = triggerId
        },
        cancellationToken);
    }

    public static Task<PaginatedResult<WorkflowExecutionApprovalsResponse>> GetWorkflowPendingApprovals(
        this IMediator mediator,
        string workflowId,
        string executionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowExecutionApprovalsRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            Page = page,
            PageSize = pageSize
        },
        cancellationToken);
    }

    public static Task<Result<Unit>> ApproveWorkflowExecution(
        this IMediator mediator,
        string workflowId,
        string executionId,
        string approvalId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new ApproveWorkflowRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            WorkflowExecutionApprovalId = approvalId
        },
        cancellationToken);
    }

    public static Task<Result<Unit>> RejectWorkflowExecution(
        this IMediator mediator,
        string workflowId,
        string executionId,
        string approvalId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new RejectWorkflowRequest
        {
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            WorkflowExecutionApprovalId = approvalId
        },
        cancellationToken);
    }

    public static Task<Result<SummaryResponse>> GetWorkflowSummary(
        this IMediator mediator,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new SummaryRequest(), cancellationToken);
    }
    #endregion

    #region Version
    public static Task<Result<VersionResponse>> Version(
        this IMediator mediator,
        VersionRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Plugins
    public static Task<PaginatedResult<PluginsListResponse>> PluginsList(
        this IMediator mediator,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<PluginDetailsResponse>> PluginDetails(
        this IMediator mediator,
        string pluginId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginDetailsRequest { PluginId = pluginId }, cancellationToken);
    }

    public static Task<Result<Unit>> InstallPlugin(
        this IMediator mediator,
        InstallPluginRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UpdatePlugin(
        this IMediator mediator,
        UpdatePluginRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UninstallPlugin(
        this IMediator mediator,
        UninstallPluginRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region PluginConfig
    public static Task<PaginatedResult<PluginConfigListResponse>> PluginsConfiguration(
        this IMediator mediator,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginConfigListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<PluginConfigDetailsResponse>> PluginConfigurationDetails(
        this IMediator mediator,
         string configId,
         CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginConfigDetailsRequest { ConfigId = configId}, cancellationToken);
    }

    public static Task<Result<AddPluginConfigResponse>> AddPluginConfiguration(
        this IMediator mediator,
        AddPluginConfigRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UpdatePluginConfiguration(
        this IMediator mediator,
        UpdatePluginConfigRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    public static Task<Result<Unit>> DeletePluginConfiguration(
        this IMediator mediator,
        string configId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeletePluginConfigRequest { ConfigId = configId }, cancellationToken);
    }
    #endregion

    #region Logs
    public static Task<PaginatedResult<LogsListResponse>> Logs(
        this IMediator mediator,
        LogsListRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Audits
    public static Task<PaginatedResult<AuditsListResponse>> Audits(
        this IMediator mediator,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new AuditsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<AuditDetailsResponse>> AuditDetails(
        this IMediator mediator,
        string auditId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new AuditDetailsRequest { AuditId = auditId }, cancellationToken);
    }
    #endregion
}
