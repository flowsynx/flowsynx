using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionDetails;

internal class WorkflowTaskExecutionDetailsHandler : 
    IRequestHandler<WorkflowTaskExecutionDetailsRequest, Result<WorkflowTaskExecutionDetailsResponse>>
{
    private readonly ILogger<WorkflowTaskExecutionDetailsHandler> _logger;
    private readonly IWorkflowTaskExecutionService _workflowTaskExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowTaskExecutionDetailsHandler(
        ILogger<WorkflowTaskExecutionDetailsHandler> logger,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTaskExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowTaskExecutionService = workflowTaskExecutionService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkflowTaskExecutionDetailsResponse>> Handle(
        WorkflowTaskExecutionDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowTaskExecutionId = Guid.Parse(request.WorkflowTaskExecutionId);

            var workflowTaskExecution = await _workflowTaskExecutionService.Get(workflowId, 
                workflowExecutionId, workflowTaskExecutionId, cancellationToken);

            if (workflowTaskExecution is null)
            {
                var message = string.Format(Resources.Feature_Workflow_Details_WorkflowNotFound, request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var response = new WorkflowTaskExecutionDetailsResponse
            {
                Id = workflowTaskExecution.Id,
                Status = workflowTaskExecution.Status,
                Message = workflowTaskExecution.Message,
                StartTime = workflowTaskExecution.StartTime,
                EndTime = workflowTaskExecution.EndTime,
            };
            _logger.LogInformation(Resources.Feature_Workflow_Details_DataRetrievedSuccessfully);
            return await Result<WorkflowTaskExecutionDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowTaskExecutionDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}