using FlowSynx.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain;
using FlowSynx.Domain.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

internal class WorkflowExecutionListHandler : IRequestHandler<WorkflowExecutionListRequest, 
    PaginatedResult<WorkflowExecutionListResponse>>
{
    private readonly ILogger<WorkflowExecutionListHandler> _logger;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowExecutionListHandler(
        ILogger<WorkflowExecutionListHandler> logger,
        IWorkflowExecutionService workflowExecutionService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowExecutionService = workflowExecutionService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<PaginatedResult<WorkflowExecutionListResponse>> Handle(
        WorkflowExecutionListRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var executions = await _workflowExecutionService.All(_currentUserService.UserId(), 
                workflowId, cancellationToken);

            var response = executions.Select(execution => new WorkflowExecutionListResponse
            {
                Id = execution.Id,
                Status = execution.Status,
                ExecutionStart = execution.ExecutionStart,
                ExecutionEnd = execution.ExecutionEnd,
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_List_RetrievedSuccessfully"));
            return await PaginatedResult<WorkflowExecutionListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await PaginatedResult<WorkflowExecutionListResponse>.FailureAsync(ex.ToString());
        }
    }
}

