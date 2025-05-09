using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflowTrigger;

internal class AddWorkflowTriggerHandler : IRequestHandler<AddWorkflowTriggerRequest, Result<AddWorkflowTriggerResponse>>
{
    private readonly ILogger<AddWorkflowTriggerHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;

    public AddWorkflowTriggerHandler(
        ILogger<AddWorkflowTriggerHandler> logger,
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

    public async Task<Result<AddWorkflowTriggerResponse>> Handle(AddWorkflowTriggerRequest request, CancellationToken cancellationToken)
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

            var workflowTriggerEntity = new WorkflowTriggerEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId,
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
                Resources.Feature_Workflow_Add_AddedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<AddWorkflowTriggerResponse>.FailAsync(ex.ToString());
        }
    }
}