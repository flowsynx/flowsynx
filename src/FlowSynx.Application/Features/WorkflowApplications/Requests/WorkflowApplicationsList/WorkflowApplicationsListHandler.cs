using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationsList;

internal class WorkflowApplicationsListHandler : IActionHandler<WorkflowApplicationsListRequest, PaginatedResult<WorkflowApplicationsListResult>>
{
    private readonly ILogger<WorkflowApplicationsListHandler> _logger;
    private readonly IWorkflowApplicationRepository _workflowApplicationRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowApplicationsListHandler(
        ILogger<WorkflowApplicationsListHandler> logger, 
        IWorkflowApplicationRepository workflowApplicationRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowApplicationRepository = workflowApplicationRepository ?? throw new ArgumentNullException(nameof(workflowApplicationRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<WorkflowApplicationsListResult>> Handle(WorkflowApplicationsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowApplications = await _workflowApplicationRepository.GetByNamespaceAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Namespace,
                cancellationToken);

            var response = workflowApplications.Select(workflowApplication => new WorkflowApplicationsListResult
            {
                Name = workflowApplication.Name,
                Namespace = workflowApplication.Namespace,
                Description = workflowApplication.Description,
                Labels = workflowApplication.Labels,
                Annotations = workflowApplication.Annotations,
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Genomes list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<WorkflowApplicationsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GenomesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<WorkflowApplicationsListResult>.FailureAsync(ex.Message);
        }
    }
}
