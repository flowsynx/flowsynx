using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;

namespace FlowSynx.Application.Features.Workflows.Command.Execute;

internal class ExecuteWorkflowHandler : IRequestHandler<ExecuteWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<ExecuteWorkflowHandler> _logger;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteWorkflowHandler(ILogger<ExecuteWorkflowHandler> logger, IWorkflowExecutor workflowExecutor,
       ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowExecutor);
        _logger = logger;
        _workflowExecutor = workflowExecutor;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, "Access is denied. Authentication is required.");

            await _workflowExecutor.ExecuteAsync(_currentUserService.UserId, request.WorkflowId, cancellationToken);
            return await Result<Unit>.SuccessAsync("Workflow executed successfully!");
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}