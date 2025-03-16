using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.Execute;

internal class ExecuteWorkflowHandler : IRequestHandler<ExecuteWorkflowRequest, Result<object?>>
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

    public async Task<Result<object?>> Handle(ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var response = await _workflowExecutor.ExecuteAsync(_currentUserService.UserId, request.WorkflowId, cancellationToken);
            return await Result<object?>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object?>.FailAsync(new List<string> { ex.Message });
        }
    }
}