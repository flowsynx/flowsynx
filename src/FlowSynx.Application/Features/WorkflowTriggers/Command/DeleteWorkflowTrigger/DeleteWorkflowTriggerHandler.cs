using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowTriggers.Command.DeleteWorkflowTrigger;

internal class DeleteWorkflowTriggerHandler : IRequestHandler<DeleteWorkflowTriggerRequest, Result<Unit>>
{
    private readonly ILogger<DeleteWorkflowTriggerHandler> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public DeleteWorkflowTriggerHandler(
        ILogger<DeleteWorkflowTriggerHandler> logger, 
        IWorkflowTriggerService workflowTriggerService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _localization = localization;
        _workflowTriggerService = workflowTriggerService;
    }

    public async Task<Result<Unit>> Handle(DeleteWorkflowTriggerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var triggerId = Guid.Parse(request.TriggerId);
            var workflow = await _workflowTriggerService.GetByIdAsync(workflowId, triggerId, cancellationToken);
            if (workflow == null)
            {
                var message = _localization.Get("Feature_WorkflowTriggers_Delete_WorkflowNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            await _workflowTriggerService.DeleteAsync(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Workflow_Delete_DeletedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}