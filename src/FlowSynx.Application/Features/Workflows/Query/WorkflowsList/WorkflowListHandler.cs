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

internal class WorkflowListHandler : IRequestHandler<WorkflowListRequest, Result<IEnumerable<WorkflowListResponse>>>
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

    public async Task<Result<IEnumerable<WorkflowListResponse>>> Handle(WorkflowListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflows = await _workflowService.All(_currentUserService.UserId, cancellationToken);
            var response = workflows.Select(workflow => new WorkflowListResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                ModifiedDate = workflow.LastModifiedOn ?? _systemClock.UtcNow

            });
            _logger.LogInformation(_localization.Get("Feature_Workflow_ListRetrievedSuccessfully"));
            return await Result<IEnumerable<WorkflowListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowListResponse>>.FailAsync(ex.ToString());
        }
    }
}