using FlowSynx.Application.Features.PluginConfig.Query.PluginConfigList;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowTriggersList;

internal class WorkflowTriggersListHandler : IRequestHandler<WorkflowTriggersListRequest, Result<IEnumerable<WorkflowTriggersListResponse>>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _systemClock;

    public WorkflowTriggersListHandler(
        ILogger<PluginConfigListHandler> logger,
        IWorkflowTriggerService workflowTriggerService, 
        ICurrentUserService currentUserService,
        ISystemClock systemClock)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
        _systemClock = systemClock;
    }

    public async Task<Result<IEnumerable<WorkflowTriggersListResponse>>> Handle(
        WorkflowTriggersListRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, 
                    Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var triggers = await _workflowTriggerService.GetByWorkflowIdAsync(workflowId, cancellationToken);
            var response = triggers.Select(trigger => new WorkflowTriggersListResponse
            {
                Id = trigger.Id,
                Type = trigger.Type,
                Status = trigger.Status,
                Properties = trigger.Properties,

            });
            _logger.LogInformation(Resources.Feature_Workflow_ListRetrievedSuccessfully);
            return await Result<IEnumerable<WorkflowTriggersListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowTriggersListResponse>>.FailAsync(ex.ToString());
        }
    }
}