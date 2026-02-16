using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationDetails;

internal class WorkflowApplicationDetailsHandler : IActionHandler<WorkflowApplicationDetailsRequest, Result<WorkflowApplicationDetailsResult>>
{
    private readonly ILogger<WorkflowApplicationDetailsHandler> _logger;
    private readonly IWorkflowApplicationRepository _workflowApplicationRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowApplicationDetailsHandler(
        ILogger<WorkflowApplicationDetailsHandler> logger, 
        IWorkflowApplicationRepository workflowApplicationRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowApplicationRepository = workflowApplicationRepository ?? throw new ArgumentNullException(nameof(workflowApplicationRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<WorkflowApplicationDetailsResult>> Handle(WorkflowApplicationDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowApplication = await _workflowApplicationRepository.GetByIdAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Id,
                cancellationToken);

            var response = new WorkflowApplicationDetailsResult
            {
                Name = workflowApplication?.Name ?? string.Empty,
                Namespace = workflowApplication?.Namespace ?? string.Empty,
                Description = workflowApplication?.Description ?? string.Empty,
                Specification = workflowApplication?.Specification!,
                Metadata = workflowApplication?.Metadata ?? new Dictionary<string, object>(),
                Labels = workflowApplication?.Labels ?? new Dictionary<string, string>(),
                Annotations = workflowApplication?.Annotations ?? new Dictionary<string, string>()
            };

            _logger.LogInformation(
                "Workflow application details retrieved successfully for workflow application {WorkflowApplicationId}.",
                request.Id);

            return await Result<WorkflowApplicationDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in WorkflowApplicationDetailsHandler for workflow application {WorkflowApplicationId}.",
                request.Id);
            return await Result<WorkflowApplicationDetailsResult>.FailAsync(ex.Message);
        }
    }
}
