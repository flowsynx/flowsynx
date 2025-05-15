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

    public CancelWorkflowHandler(
        ILogger<CancelWorkflowHandler> logger,
        IWorkflowCancellationRegistry workflowCancellationRegistry,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowCancellationRegistry);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowCancellationRegistry = workflowCancellationRegistry;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(CancelWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);

            _workflowCancellationRegistry.Cancel(_currentUserService.UserId, workflowId, workflowExecutionId);
            return await Result<Unit>.SuccessAsync(string.Format(Resources.Feature_WorkflowExecution_CancelledSuccessfully, workflowId));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}