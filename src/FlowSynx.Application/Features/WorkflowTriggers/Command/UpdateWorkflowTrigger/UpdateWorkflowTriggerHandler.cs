using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowTriggers.Command.UpdateWorkflowTrigger;

internal class UpdateWorkflowTriggerHandler : IRequestHandler<UpdateWorkflowTriggerRequest, Result<Unit>>
{
    private readonly ILogger<UpdateWorkflowTriggerHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public UpdateWorkflowTriggerHandler(
        ILogger<UpdateWorkflowTriggerHandler> logger,
        IWorkflowService workflowService,
        IWorkflowTriggerService workflowTriggerService, 
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowService = workflowService;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(UpdateWorkflowTriggerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId(), workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = _localization.Get("Feature_WorkflowTriggers_Update_WorkflowNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var triggerId = Guid.Parse(request.TriggerId);
            var trigger = await _workflowTriggerService.GetByIdAsync(workflowId, triggerId, cancellationToken);
            if (trigger == null)
            {
                var message = _localization.Get("Feature_WorkflowTriggers_Update_TriggerNotFound", request.TriggerId);
                throw new FlowSynxException((int)ErrorCode.WorkflowTriggerNotFound, message);
            }

            trigger.Status = request.Status;
            trigger.Type = request.Type;
            trigger.Properties = request.Properties;

            await _workflowTriggerService.UpdateAsync(trigger, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowTrigger_UpdatedSuccessfully", triggerId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}
