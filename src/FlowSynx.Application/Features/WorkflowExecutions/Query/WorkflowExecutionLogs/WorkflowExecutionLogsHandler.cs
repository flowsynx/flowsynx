using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;

internal class WorkflowExecutionLogsHandler : IRequestHandler<WorkflowExecutionLogsRequest, 
    Result<IEnumerable<WorkflowExecutionLogsResponse>>>
{
    private readonly ILogger<WorkflowExecutionLogsHandler> _logger;
    private readonly ILoggerService _loggerService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowExecutionLogsHandler(
        ILogger<WorkflowExecutionLogsHandler> logger,
        ILoggerService loggerService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _loggerService = loggerService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IEnumerable<WorkflowExecutionLogsResponse>>> Handle(
        WorkflowExecutionLogsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, 
                    Resources.Authentication_Access_Denied);

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
            _logger.LogInformation(Resources.Feature_Workflow_Details_DataRetrievedSuccessfully);
            return await Result<IEnumerable<WorkflowExecutionLogsResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowExecutionLogsResponse>>.FailAsync(ex.ToString());
        }
    }
}