using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowActivitiesList;

internal class WorkflowActivitiesListHandler : IActionHandler<WorkflowActivitiesListRequest, PaginatedResult<WorkflowActivitiesListResult>>
{
    private readonly ILogger<WorkflowActivitiesListHandler> _logger;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowActivitiesListHandler(
        ILogger<WorkflowActivitiesListHandler> logger, 
        IWorkflowRepository workflowRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<WorkflowActivitiesListResult>> Handle(WorkflowActivitiesListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowExists = await _workflowRepository.Exist(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.WorkflowId,
                cancellationToken);

            if (!workflowExists)
            {
                _logger.LogWarning("No workflow found with WorkflowId '{WorkflowId}'.", request.WorkflowId);
                return await PaginatedResult<WorkflowActivitiesListResult>.FailureAsync($"No workflow found with WorkflowId {request.WorkflowId}.");
            }

            var workflowActivities = await _workflowRepository.GetWorkflowActivitiesAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.WorkflowId,
                cancellationToken);

            var response = workflowActivities.Select(activity => new WorkflowActivitiesListResult
            {
                Name = activity.Activity.Name
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Workflow activities list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<WorkflowActivitiesListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in WorkflowActivitiesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<WorkflowActivitiesListResult>.FailureAsync(ex.Message);
        }
    }
}
