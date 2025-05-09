using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Log;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

internal class WorkflowExecutionListHandler : IRequestHandler<WorkflowExecutionListRequest, 
    Result<IEnumerable<WorkflowExecutionListResponse>>>
{
    private readonly ILogger<WorkflowExecutionListHandler> _logger;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowExecutionListHandler(
        ILogger<WorkflowExecutionListHandler> logger,
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

    public async Task<Result<IEnumerable<WorkflowExecutionListResponse>>> Handle(
        WorkflowExecutionListRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, 
                    Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var executions = await _workflowExecutionService.All(_currentUserService.UserId, 
                workflowId, cancellationToken);

            var response = executions.Select(execution => new WorkflowExecutionListResponse
            {
                Id = execution.Id,
                Status = execution.Status,
                ExecutionStart = execution.ExecutionStart,
                ExecutionEnd = execution.ExecutionEnd,
            });
            _logger.LogInformation(Resources.Feature_Workflow_Details_DataRetrievedSuccessfully);
            return await Result<IEnumerable<WorkflowExecutionListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowExecutionListResponse>>.FailAsync(ex.ToString());
        }
    }
}