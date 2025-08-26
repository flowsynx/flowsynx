using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

internal class ExecuteWorkflowHandler : IRequestHandler<ExecuteWorkflowRequest, Result<Guid>>
{
    private readonly ILogger<ExecuteWorkflowHandler> _logger;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly IWorkflowExecutionQueue _workflowExecutionQueue;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public ExecuteWorkflowHandler(
        ILogger<ExecuteWorkflowHandler> logger, 
        IWorkflowOrchestrator workflowOrchestrator,
        IWorkflowExecutionQueue workflowExecutionQueue,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        ArgumentNullException.ThrowIfNull(workflowExecutionQueue);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowOrchestrator = workflowOrchestrator;
        _workflowExecutionQueue = workflowExecutionQueue;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<Guid>> Handle(ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var result = await _workflowOrchestrator.CreateWorkflowExecutionAsync(
                _currentUserService.UserId, 
                workflowId, 
                cancellationToken);

            await _workflowExecutionQueue.QueueExecutionAsync(new ExecutionQueueRequest(
                _currentUserService.UserId,
                workflowId,
                result.Id,
                cancellationToken), cancellationToken);

            return await Result<Guid>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_ExecutedSuccessfully", workflowId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Guid>.FailAsync(ex.ToString());
        }
    }
}