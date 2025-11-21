using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.RejectWorkflow;

internal class RejectWorkflowHandler : IRequestHandler<RejectWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<RejectWorkflowHandler> _logger;
    private readonly IManualApprovalService _manualApprovalService;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _systemClock;
    private readonly ILocalization _localization;

    public RejectWorkflowHandler(
        ILogger<RejectWorkflowHandler> logger,
        IManualApprovalService manualApprovalService,
        IWorkflowExecutionService workflowExecutionService,
        ICurrentUserService currentUserService,
        ISystemClock systemClock,
        ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _manualApprovalService = manualApprovalService ?? throw new ArgumentNullException(nameof(manualApprovalService));
        _workflowExecutionService = workflowExecutionService ?? throw new ArgumentNullException(nameof(workflowExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public async Task<Result<Unit>> Handle(RejectWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowExecutionApprovalId = Guid.Parse(request.WorkflowExecutionApprovalId);

            await _manualApprovalService.RejectAsync(_currentUserService.UserId(), workflowId, 
                workflowExecutionId, workflowExecutionApprovalId, cancellationToken);

            await UpdateWorkflowExecutionAsFailedAsync(_currentUserService.UserId(), workflowId, workflowExecutionId, cancellationToken).ConfigureAwait(false);

            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_ManualApprovalRejected", workflowExecutionApprovalId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }

    private async Task UpdateWorkflowExecutionAsFailedAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        CancellationToken cancellationToken)
    {
        var execution = await _workflowExecutionService.Get(userId, workflowId, executionId, cancellationToken);
        if (execution == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionRejected,
                _localization.Get("Workflow_Orchestrator_WorkflowNotPaused", executionId));

        execution.ExecutionEnd = _systemClock.UtcNow;
        execution.Status = WorkflowExecutionStatus.Failed;
        await _workflowExecutionService.Update(execution, cancellationToken);
    }
}
