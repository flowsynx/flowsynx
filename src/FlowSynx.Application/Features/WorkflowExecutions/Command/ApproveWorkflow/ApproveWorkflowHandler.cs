using FlowSynx.Application.Localizations;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ApproveWorkflow;

internal class ApproveWorkflowHandler : IRequestHandler<ApproveWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<ApproveWorkflowHandler> _logger;
    private readonly IManualApprovalService _manualApprovalService;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public ApproveWorkflowHandler(
        ILogger<ApproveWorkflowHandler> logger,
        IManualApprovalService manualApprovalService,
        IWorkflowOrchestrator workflowOrchestrator,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(manualApprovalService);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _manualApprovalService = manualApprovalService;
        _workflowOrchestrator = workflowOrchestrator;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(ApproveWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowExecutionApprovalId = Guid.Parse(request.WorkflowExecutionApprovalId);

            await _manualApprovalService.ApproveAsync(_currentUserService.UserId, workflowId, 
                workflowExecutionId, workflowExecutionApprovalId, cancellationToken);

            var result = await _workflowOrchestrator.ResumeWorkflowAsync(_currentUserService.UserId, workflowId, 
                workflowExecutionId, cancellationToken);

            if (result == Domain.Workflow.WorkflowExecutionStatus.Paused)
                return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_PausedForManualApproval", workflowId));

            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_ResumedSuccessfully", workflowId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}