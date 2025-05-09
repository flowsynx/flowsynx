using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Trigger;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowTriggerDetails;

internal class WorkflowTriggerDetailsHandler : IRequestHandler<WorkflowTriggerDetailsRequest, Result<WorkflowTriggerDetailsResponse>>
{
    private readonly ILogger<WorkflowTriggerDetailsHandler> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowTriggerDetailsHandler(
        ILogger<WorkflowTriggerDetailsHandler> logger,
        IWorkflowTriggerService workflowTriggerService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkflowTriggerDetailsResponse>> Handle(WorkflowTriggerDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var triggerId = Guid.Parse(request.TriggerId);
            var trigger = await _workflowTriggerService.GetByIdAsync(workflowId, triggerId, cancellationToken);
            if (trigger is null)
            {
                var message = string.Format(Resources.Feature_Workflow_Details_WorkflowNotFound, request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var response = new WorkflowTriggerDetailsResponse
            {
                Id = trigger.Id,
                Type = trigger.Type,
                Status = trigger.Status,
                Properties = trigger.Properties
            };
            _logger.LogInformation(Resources.Feature_Workflow_Details_DataRetrievedSuccessfully);
            return await Result<WorkflowTriggerDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowTriggerDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}