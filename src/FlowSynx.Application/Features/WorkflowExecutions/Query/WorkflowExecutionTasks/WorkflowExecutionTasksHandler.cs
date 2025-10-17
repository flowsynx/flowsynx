using FlowSynx.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionTasks;

internal class WorkflowExecutionTasksHandler : IRequestHandler<WorkflowExecutionTasksRequest, PaginatedResult<WorkflowExecutionTasksResponse>>
{
    private readonly ILogger<WorkflowExecutionTasksHandler> _logger;
    private readonly IWorkflowTaskExecutionService _workflowTaskExecutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowExecutionTasksHandler(
        ILogger<WorkflowExecutionTasksHandler> logger,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTaskExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowTaskExecutionService = workflowTaskExecutionService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<PaginatedResult<WorkflowExecutionTasksResponse>> Handle(
        WorkflowExecutionTasksRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowExecutionTasks = await _workflowTaskExecutionService.All(workflowId, 
                workflowExecutionId, cancellationToken);

            if (workflowExecutionTasks is null)
            {
                var message = _localization.Get("Feature_WorkflowExecution_Details_ExecutionNotFound", request.WorkflowExecutionId);
                throw new FlowSynxException((int)ErrorCode.WorkflowExecutionNotFound, message);
            }

            var response = workflowExecutionTasks.Select(t => new WorkflowExecutionTasksResponse
            {
                Id = t.Id,
                WorkflowId = t.WorkflowId,
                WorkflowExecutionId = t.WorkflowExecutionId,
                Name = t.Name,
                Status = t.Status,
                Message = t.Message,
                StartTime = t.StartTime,
                EndTime = t.EndTime
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_Details_DataRetrievedSuccessfully"));
            return await PaginatedResult<WorkflowExecutionTasksResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await PaginatedResult<WorkflowExecutionTasksResponse>.FailureAsync(ex.ToString());
        }
    }
}
