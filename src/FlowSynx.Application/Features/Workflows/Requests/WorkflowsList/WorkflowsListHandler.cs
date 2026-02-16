using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowsList;

internal class WorkflowsListHandler : IActionHandler<WorkflowsListRequest, PaginatedResult<WorkflowsListResult>>
{
    private readonly ILogger<WorkflowsListHandler> _logger;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowsListHandler(
        ILogger<WorkflowsListHandler> logger, 
        IWorkflowRepository workflowRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<WorkflowsListResult>> Handle(WorkflowsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflows = await _workflowRepository.GetByNamespaceAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Namespace,
                cancellationToken);

            var response = workflows.Select(workflow => new WorkflowsListResult
            {
                Name = workflow.Name,
                Namespace = workflow.Namespace,
                Description = workflow.Description,
                Labels = workflow.Labels,
                Annotations = workflow.Annotations,
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Workflows list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<WorkflowsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in WorkflowsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<WorkflowsListResult>.FailureAsync(ex.Message);
        }
    }
}
