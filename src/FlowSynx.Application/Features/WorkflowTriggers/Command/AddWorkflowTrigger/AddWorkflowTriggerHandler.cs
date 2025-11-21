using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowTriggers.Command.AddWorkflowTrigger;

internal class AddWorkflowTriggerHandler : IRequestHandler<AddWorkflowTriggerRequest, Result<AddWorkflowTriggerResponse>>
{
    private readonly ILogger<AddWorkflowTriggerHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public AddWorkflowTriggerHandler(
        ILogger<AddWorkflowTriggerHandler> logger,
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

    public async Task<Result<AddWorkflowTriggerResponse>> Handle(AddWorkflowTriggerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId(), workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = _localization.Get("Feature_WorkflowTriggers_Add_WorkflowNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var workflowTriggerEntity = new WorkflowTriggerEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId(),
                WorkflowId = workflowId,
                Type = request.Type,
                Properties = request.Properties,
                Status = request.Status
            };
            await _workflowTriggerService.AddAsync(workflowTriggerEntity, cancellationToken);

            var response = new AddWorkflowTriggerResponse
            {
                TriggerId = workflowTriggerEntity.Id
            };

            return await Result<AddWorkflowTriggerResponse>.SuccessAsync(response,
                _localization.Get("Feature_WorkflowTriggers_AddedSuccessfully", workflowTriggerEntity.Id));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<AddWorkflowTriggerResponse>.FailAsync(ex.ToString());
        }
    }
}
