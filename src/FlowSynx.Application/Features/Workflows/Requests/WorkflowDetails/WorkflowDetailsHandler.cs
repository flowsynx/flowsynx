using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowDetails;

internal class WorkflowDetailsHandler : IActionHandler<WorkflowDetailsRequest, Result<WorkflowDetailsResult>>
{
    private readonly ILogger<WorkflowDetailsHandler> _logger;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowDetailsHandler(
        ILogger<WorkflowDetailsHandler> logger, 
        IWorkflowRepository workflowRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<WorkflowDetailsResult>> Handle(WorkflowDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflow = await _workflowRepository.GetByIdAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Id,
                cancellationToken);

            var response = new WorkflowDetailsResult
            {
                Name = workflow?.Name ?? string.Empty,
                Namespace = workflow?.Namespace ?? string.Empty,
                Description = workflow?.Description ?? string.Empty,
                Specification = workflow?.Specification!,
                Metadata = workflow?.Metadata ?? new Dictionary<string, object>(),
                Labels = workflow?.Labels ?? new Dictionary<string, string>(),
                Annotations = workflow?.Annotations ?? new Dictionary<string, string>()
            };

            _logger.LogInformation(
                "Workflow details retrieved successfully for workflow {WorkflowId}.",
                request.Id);

            return await Result<WorkflowDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in WorkflowDetailsHandler for workflow {WorkflowId}.",
                request.Id);
            return await Result<WorkflowDetailsResult>.FailAsync(ex.Message);
        }
    }
}
