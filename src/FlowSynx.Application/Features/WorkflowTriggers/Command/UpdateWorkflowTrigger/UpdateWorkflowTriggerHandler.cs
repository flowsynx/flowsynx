using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.UpdateWorkflowTrigger;

internal class UpdateWorkflowTriggerHandler : IRequestHandler<UpdateWorkflowTriggerRequest, Result<Unit>>
{
    private readonly ILogger<UpdateWorkflowTriggerHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateWorkflowTriggerHandler(
        ILogger<UpdateWorkflowTriggerHandler> logger,
        IWorkflowService workflowService,
        IWorkflowTriggerService workflowTriggerService, 
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(UpdateWorkflowTriggerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, 
                    Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = string.Format(Resources.Features_Workflow_Delete_WorkflowCouldNotBeFound, request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var triggerId = Guid.Parse(request.TriggerId);
            var trigger = await _workflowTriggerService.GetByIdAsync(workflowId, triggerId, cancellationToken);
            if (trigger == null)
            {
                var message = string.Format(Resources.Features_Workflow_Delete_WorkflowCouldNotBeFound, request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            trigger.Status = request.Status;
            trigger.Type = request.Type;
            trigger.Properties = request.Properties;

            await _workflowTriggerService.UpdateAsync(trigger, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.Feature_Workflow_Add_AddedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}