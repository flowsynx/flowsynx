using FlowSynx.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionApprovals;

internal class WorkflowExecutionApprovalsHandler : IRequestHandler<WorkflowExecutionApprovalsRequest, 
    PaginatedResult<WorkflowExecutionApprovalsResponse>>
{
    private readonly ILogger<WorkflowExecutionApprovalsHandler> _logger;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowExecutionApprovalsHandler(
        ILogger<WorkflowExecutionApprovalsHandler> logger,
        IWorkflowApprovalService workflowApprovalService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowApprovalService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowApprovalService = workflowApprovalService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<PaginatedResult<WorkflowExecutionApprovalsResponse>> Handle(
        WorkflowExecutionApprovalsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowApprovals = await _workflowApprovalService.GetPendingApprovalsAsync(_currentUserService.UserId(), 
                workflowId, workflowExecutionId, cancellationToken);

            var response = workflowApprovals.Select(l => new WorkflowExecutionApprovalsResponse
            {
                Id = l.Id,
                WorkflowId = l.WorkflowId,
                ExecutionId = l.ExecutionId,
                TaskName = l.TaskName,
                RequestedBy = l.RequestedBy,
                RequestedAt = l.RequestedAt,
                Comments = l.Comments,
                Status = l.Status.ToString()
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_Logs_DataRetrievedSuccessfully"));
            return await PaginatedResult<WorkflowExecutionApprovalsResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await PaginatedResult<WorkflowExecutionApprovalsResponse>.FailureAsync(ex.ToString());
        }
    }
}

