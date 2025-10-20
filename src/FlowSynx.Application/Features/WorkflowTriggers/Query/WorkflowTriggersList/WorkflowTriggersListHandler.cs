using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Query.PluginConfigList;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Trigger;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowTriggers.Query.WorkflowTriggersList;

internal class WorkflowTriggersListHandler : IRequestHandler<WorkflowTriggersListRequest, PaginatedResult<WorkflowTriggersListResponse>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _systemClock;
    private readonly ILocalization _localization;

    public WorkflowTriggersListHandler(
        ILogger<PluginConfigListHandler> logger,
        IWorkflowTriggerService workflowTriggerService, 
        ICurrentUserService currentUserService,
        ISystemClock systemClock,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
        _systemClock = systemClock;
        _localization = localization;
    }

    public async Task<PaginatedResult<WorkflowTriggersListResponse>> Handle(
        WorkflowTriggersListRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var triggers = await _workflowTriggerService.GetByWorkflowIdAsync(workflowId, cancellationToken);
            var response = triggers.Select(trigger => new WorkflowTriggersListResponse
            {
                Id = trigger.Id,
                Type = trigger.Type,
                Status = trigger.Status,
                Properties = trigger.Properties,
                LastModified = trigger.LastModifiedOn ?? trigger.CreatedOn

            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(_localization.Get("Feature_WorkflowTriggers_List_RetrievedSuccessfully", workflowId));
            return await PaginatedResult<WorkflowTriggersListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await PaginatedResult<WorkflowTriggersListResponse>.FailureAsync(ex.ToString());
        }
    }
}
