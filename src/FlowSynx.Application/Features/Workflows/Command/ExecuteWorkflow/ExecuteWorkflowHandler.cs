using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;

namespace FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

internal class ExecuteWorkflowHandler : IRequestHandler<ExecuteWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<ExecuteWorkflowHandler> _logger;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteWorkflowHandler(ILogger<ExecuteWorkflowHandler> logger, IWorkflowOrchestrator workflowOrchestrator,
       ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        _logger = logger;
        _workflowOrchestrator = workflowOrchestrator;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired,
                    Resources.Authentication_Access_Denied);

            await _workflowOrchestrator.ExecuteWorkflowAsync(_currentUserService.UserId, request.WorkflowId, cancellationToken);
            return await Result<Unit>.SuccessAsync("Workflow executed successfully!");
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}