using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Metrics.Query;

internal class SummaryHandler : IRequestHandler<SummaryRequest, Result<SummaryResponse>>
{
    private readonly ILogger<SummaryHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public SummaryHandler(
        ILogger<SummaryHandler> logger,
        IWorkflowService workflowService,
        IWorkflowExecutionService workflowExecutionService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(workflowExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _workflowExecutionService = workflowExecutionService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SummaryResponse>> Handle(SummaryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();
            var userId = _currentUserService.UserId();

            var response = new SummaryResponse()
            {
                ActiveWorkflows = await _workflowService.GetActiveWorkflowsCountAsync(userId, cancellationToken),
                RunningTasks = await _workflowExecutionService.GetRunningWorkflowCountAsync(userId, cancellationToken),
                CompletedToday = await _workflowExecutionService.GetCompletedWorkflowsCountAsync(userId, cancellationToken),
                FailedWorkflows = await _workflowExecutionService.GetFailedWorkflowsCountAsync(userId, cancellationToken)
            };

            return await Result<SummaryResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught in SummaryHandler.");
            return await Result<SummaryResponse>.FailAsync(ex.Message);
        }
    }
}
