using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

internal class ExecuteWorkflowHandler : IRequestHandler<ExecuteWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<ExecuteWorkflowHandler> _logger;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public ExecuteWorkflowHandler(
        ILogger<ExecuteWorkflowHandler> logger, 
        IWorkflowOrchestrator workflowOrchestrator,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowOrchestrator = workflowOrchestrator;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            await _workflowOrchestrator.ExecuteWorkflowAsync(_currentUserService.UserId, workflowId, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_ExecutedSuccessfully", workflowId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}