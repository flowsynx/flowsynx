using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowExecutionQueue
{
    ValueTask QueueExecutionAsync(ExecutionQueueRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<ExecutionQueueRequest> DequeueAllAsync(CancellationToken cancellationToken);
    Task MarkAsCompletedAsync(Guid executionId, CancellationToken cancellationToken);
    Task MarkAsFailedAsync(Guid executionId, CancellationToken cancellationToken);
}