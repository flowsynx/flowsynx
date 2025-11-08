using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Query.PluginConfigList;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowsList;

internal class WorkflowListHandler : IRequestHandler<WorkflowListRequest, PaginatedResult<WorkflowListResponse>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _systemClock;
    private readonly ILocalization _localization;

    public WorkflowListHandler(
        ILogger<PluginConfigListHandler> logger,
        IWorkflowService workflowService, 
        ICurrentUserService currentUserService,
        ISystemClock systemClock,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _systemClock = systemClock;
        _localization = localization;
    }

    public async Task<PaginatedResult<WorkflowListResponse>> Handle(WorkflowListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflows = await _workflowService.All(_currentUserService.UserId(), cancellationToken);
            var response = workflows.Select(workflow => new WorkflowListResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                ModifiedDate = workflow.LastModifiedOn ?? _systemClock.UtcNow,
                SchemaUrl = workflow.SchemaUrl

            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(_localization.Get("Feature_Workflow_ListRetrievedSuccessfully"));
            return await PaginatedResult<WorkflowListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await PaginatedResult<WorkflowListResponse>.FailureAsync(ex.ToString());
        }
    }
}

