using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;

internal class WorkflowExecutionDetailsHandler : IRequestHandler<WorkflowExecutionDetailsRequest, Result<WorkflowExecutionDetailsResponse>>
{
    private readonly ILogger<WorkflowExecutionDetailsHandler> _logger;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowExecutionDetailsHandler(
        ILogger<WorkflowExecutionDetailsHandler> logger,
        IWorkflowExecutionService workflowExecutionService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowExecutionService = workflowExecutionService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkflowExecutionDetailsResponse>> Handle(WorkflowExecutionDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowExecution = await _workflowExecutionService.Get(_currentUserService.UserId, 
                workflowId, workflowExecutionId, cancellationToken);

            if (workflowExecution is null)
            {
                var message = string.Format(Resources.Feature_WorkflowExecution_Details_ExecutionNotFound, request.WorkflowExecutionId);
                throw new FlowSynxException((int)ErrorCode.WorkflowExecutionNotFound, message);
            }

            var response = new WorkflowExecutionDetailsResponse
            {
                Id = workflowExecution.Id,
                Status = workflowExecution.Status,
                ExecutionStart = workflowExecution.ExecutionStart,
                ExecutionEnd = workflowExecution.ExecutionEnd,
            };
            _logger.LogInformation(Resources.Feature_WorkflowExecution_Details_DataRetrievedSuccessfully);
            return await Result<WorkflowExecutionDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowExecutionDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}