using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;

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
                throw new UnauthorizedAccessException("User is not authenticated.");

            await _workflowExecutor.ExecuteAsync(_currentUserService.UserId, request.WorkflowId, cancellationToken);
            return await Result<Unit>.SuccessAsync("Workflow executed successfully!");
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}