using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowExecutionQueue
{
    ValueTask EnqueueAsync(ExecutionQueueRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<ExecutionQueueRequest> DequeueAllAsync(CancellationToken cancellationToken);
    Task CompleteAsync(Guid executionId, CancellationToken cancellationToken);
    Task FailAsync(Guid executionId, CancellationToken cancellationToken);
}