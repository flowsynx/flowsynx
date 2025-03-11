using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.Workflows.Command.Execute;

internal class WorkflowHandler : IRequestHandler<WorkflowRequest, Result<object?>>
{
    private readonly ILogger<WorkflowHandler> _logger;
    private readonly IWorkflowExecutor _workflowExecutor;

    public WorkflowHandler(ILogger<WorkflowHandler> logger, IWorkflowExecutor workflowExecutor)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowExecutor);
        _logger = logger;
        _workflowExecutor = workflowExecutor;
    }

    public async Task<Result<object?>> Handle(WorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _workflowExecutor.ExecuteAsync(request.WorkflowDefinition, cancellationToken);
            return await Result<object?>.SuccessAsync("ok");
        }
        catch (Exception ex)
        {
            return await Result<object?>.FailAsync(new List<string> { ex.Message });
        }
    }
}