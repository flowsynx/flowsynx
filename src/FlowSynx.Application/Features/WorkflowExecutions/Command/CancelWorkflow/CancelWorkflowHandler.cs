using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.CancelWorkflow;

internal class CancelWorkflowHandler : IRequestHandler<CancelWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<CancelWorkflowHandler> _logger;
    private readonly IWorkflowCancellationRegistry _workflowCancellationRegistry;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public CancelWorkflowHandler(
        ILogger<CancelWorkflowHandler> logger,
        IWorkflowCancellationRegistry workflowCancellationRegistry,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowCancellationRegistry);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowCancellationRegistry = workflowCancellationRegistry;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(CancelWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);

            _workflowCancellationRegistry.Cancel(_currentUserService.UserId(), workflowId, workflowExecutionId);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_CancelledSuccessfully", workflowId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}
