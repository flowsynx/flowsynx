using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;

internal class WorkflowExecutionDetailsHandler : IRequestHandler<WorkflowExecutionDetailsRequest, Result<WorkflowExecutionDetailsResponse>>
{
    private readonly ILogger<WorkflowExecutionDetailsHandler> _logger;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowExecutionDetailsHandler(
        ILogger<WorkflowExecutionDetailsHandler> logger,
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

    public async Task<Result<WorkflowExecutionDetailsResponse>> Handle(
        WorkflowExecutionDetailsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowExecution = await _workflowExecutionService.Get(_currentUserService.UserId, 
                workflowId, workflowExecutionId, cancellationToken);

            if (workflowExecution is null)
            {
                var message = _localization.Get("Feature_WorkflowExecution_Details_ExecutionNotFound", request.WorkflowExecutionId);
                throw new FlowSynxException((int)ErrorCode.WorkflowExecutionNotFound, message);
            }

            var response = new WorkflowExecutionDetailsResponse
            {
                WorkflowId = workflowId,
                ExecutionId = workflowExecution.Id,
                Workflow = workflowExecution.WorkflowDefinition,
                Status = workflowExecution.Status,
                ExecutionStart = workflowExecution.ExecutionStart,
                ExecutionEnd = workflowExecution.ExecutionEnd,
            };
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_Details_DataRetrievedSuccessfully"));
            return await Result<WorkflowExecutionDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowExecutionDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}