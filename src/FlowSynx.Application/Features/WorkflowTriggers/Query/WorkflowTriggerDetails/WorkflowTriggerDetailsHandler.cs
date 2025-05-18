using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Trigger;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowTriggerDetails;

internal class WorkflowTriggerDetailsHandler : IRequestHandler<WorkflowTriggerDetailsRequest, Result<WorkflowTriggerDetailsResponse>>
{
    private readonly ILogger<WorkflowTriggerDetailsHandler> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowTriggerDetailsHandler(
        ILogger<WorkflowTriggerDetailsHandler> logger,
        IWorkflowTriggerService workflowTriggerService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<WorkflowTriggerDetailsResponse>> Handle(WorkflowTriggerDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var triggerId = Guid.Parse(request.TriggerId);
            var trigger = await _workflowTriggerService.GetByIdAsync(workflowId, triggerId, cancellationToken);
            if (trigger is null)
            {
                var message = _localization.Get("Feature_WorkflowTriggers_Details_TriggerNotFound", request.TriggerId);
                throw new FlowSynxException((int)ErrorCode.WorkflowTriggerNotFound, message);
            }

            var response = new WorkflowTriggerDetailsResponse
            {
                Id = trigger.Id,
                Type = trigger.Type,
                Status = trigger.Status,
                Properties = trigger.Properties
            };
            _logger.LogInformation(_localization.Get("Feature_WorkflowTriggers_Details_DataRetrievedSuccessfully"));
            return await Result<WorkflowTriggerDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowTriggerDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}