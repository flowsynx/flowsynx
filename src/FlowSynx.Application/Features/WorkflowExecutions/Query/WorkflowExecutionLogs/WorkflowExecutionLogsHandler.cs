using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;

internal class WorkflowExecutionLogsHandler : IRequestHandler<WorkflowExecutionLogsRequest, 
    Result<IEnumerable<WorkflowExecutionLogsResponse>>>
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

    public async Task<Result<IEnumerable<WorkflowExecutionLogsResponse>>> Handle(
        WorkflowExecutionLogsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var logs = await _loggerService.GetWorkflowExecutionLogs(_currentUserService.UserId, 
                workflowId, workflowExecutionId, cancellationToken);

            var response = logs.Select(l => new WorkflowExecutionLogsResponse
            {
                Id = l.Id,
                Level = l.Level,
                TimeStamp = l.TimeStamp,
                Message = l.Message,
                Exception = l.Exception
            });
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_Logs_DataRetrievedSuccessfully"));
            return await Result<IEnumerable<WorkflowExecutionLogsResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowExecutionLogsResponse>>.FailAsync(ex.ToString());
        }
    }
}