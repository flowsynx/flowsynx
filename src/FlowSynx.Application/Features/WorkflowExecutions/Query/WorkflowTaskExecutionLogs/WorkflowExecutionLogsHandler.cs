﻿using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;

internal class WorkflowTaskExecutionLogsHandler : IRequestHandler<WorkflowTaskExecutionLogsRequest, 
    Result<IEnumerable<WorkflowTaskExecutionLogsResponse>>>
{
    private readonly ILogger<WorkflowTaskExecutionLogsHandler> _logger;
    private readonly ILoggerService _loggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowTaskExecutionLogsHandler(
        ILogger<WorkflowTaskExecutionLogsHandler> logger,
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

    public async Task<Result<IEnumerable<WorkflowTaskExecutionLogsResponse>>> Handle(
        WorkflowTaskExecutionLogsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowTaskExecutionId = Guid.Parse(request.WorkflowTaskExecutionId);

            var logs = await _loggerService.GetWorkflowTaskExecutionLogs(_currentUserService.UserId, 
                workflowId, workflowExecutionId, workflowTaskExecutionId, cancellationToken);

            var response = logs.Select(l => new WorkflowTaskExecutionLogsResponse
            {
                Id = l.Id,
                Level = l.Level,
                TimeStamp = l.TimeStamp,
                Message = l.Message,
                Exception = l.Exception
            });
            _logger.LogInformation(_localization.Get("Feature_WorkflowTaskExecution_Logs_DataRetrievedSuccessfully"));
            return await Result<IEnumerable<WorkflowTaskExecutionLogsResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowTaskExecutionLogsResponse>>.FailAsync(ex.ToString());
        }
    }
}