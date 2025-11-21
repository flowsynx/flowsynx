using FlowSynx.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;

internal class WorkflowExecutionLogsHandler : IRequestHandler<WorkflowExecutionLogsRequest, 
    PaginatedResult<WorkflowExecutionLogsResponse>>
{
    private readonly ILogger<WorkflowExecutionLogsHandler> _logger;
    private readonly ILoggerService _loggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowExecutionLogsHandler(
        ILogger<WorkflowExecutionLogsHandler> logger,
        ILoggerService loggerService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _loggerService = loggerService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<PaginatedResult<WorkflowExecutionLogsResponse>> Handle(
        WorkflowExecutionLogsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var logs = await _loggerService.GetWorkflowExecutionLogs(_currentUserService.UserId(), 
                workflowId, workflowExecutionId, cancellationToken);

            var response = logs.Select(l => new WorkflowExecutionLogsResponse
            {
                Id = l.Id,
                Level = l.Level,
                TimeStamp = l.TimeStamp,
                Message = l.Message,
                Exception = l.Exception
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_Logs_DataRetrievedSuccessfully"));
            return await PaginatedResult<WorkflowExecutionLogsResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await PaginatedResult<WorkflowExecutionLogsResponse>.FailureAsync(ex.ToString());
        }
    }
}

