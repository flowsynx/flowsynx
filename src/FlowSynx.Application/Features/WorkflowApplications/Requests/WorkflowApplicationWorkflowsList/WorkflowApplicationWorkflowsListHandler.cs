using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationWorkflowsList;

internal class WorkflowApplicationWorkflowsListHandler : IActionHandler<WorkflowApplicationWorkflowsListRequest, PaginatedResult<WorkflowApplicationWorkflowsListResult>>
{
    private readonly ILogger<WorkflowApplicationWorkflowsListHandler> _logger;
    private readonly IWorkflowApplicationRepository _workflowApplicationRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowApplicationWorkflowsListHandler(
        ILogger<WorkflowApplicationWorkflowsListHandler> logger, 
        IWorkflowApplicationRepository workflowApplicationRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowApplicationRepository = workflowApplicationRepository ?? throw new ArgumentNullException(nameof(workflowApplicationRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<WorkflowApplicationWorkflowsListResult>> Handle(WorkflowApplicationWorkflowsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowApplicationExists = await _workflowApplicationRepository.Exist(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.WorkflowApplicationId,
                cancellationToken);

            if (!workflowApplicationExists)
            {
                _logger.LogWarning("No workflow application found with WorkflowId '{WorkflowApplicationId}'.", request.WorkflowApplicationId);
                return await PaginatedResult<WorkflowApplicationWorkflowsListResult>.FailureAsync($"No workflow application found with WorkflowId {request.WorkflowApplicationId}.");
            }

            var workflowApplicationWorkflows = await _workflowApplicationRepository.GetWorkflowsAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.WorkflowApplicationId,
                cancellationToken);

            var response = workflowApplicationWorkflows.Select(workflow => new WorkflowApplicationWorkflowsListResult
            {
                Name = workflow.Name
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Workflow application workflows list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<WorkflowApplicationWorkflowsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in WorkflowApplicationWorkflowsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<WorkflowApplicationWorkflowsListResult>.FailureAsync(ex.Message);
        }
    }
}
