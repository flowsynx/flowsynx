using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Workflow;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace FlowSynx.Infrastructure.Workflow;

public class InMemoryWorkflowExecutionQueue : IWorkflowExecutionQueue
{
    private readonly Channel<ExecutionQueueRequest> _queue;
    private readonly ConcurrentDictionary<Guid, ExecutionStatus> _statuses = new();

    public InMemoryWorkflowExecutionQueue(int capacity = 100)
    {
        _queue = Channel.CreateBounded<ExecutionQueueRequest>(capacity);
    }

    public async ValueTask QueueExecutionAsync(
        ExecutionQueueRequest request, 
        CancellationToken cancellationToken)
    {
        _statuses[request.ExecutionId] = ExecutionStatus.Pending;
        await _queue.Writer.WriteAsync(request, cancellationToken);
    }

    public async IAsyncEnumerable<ExecutionQueueRequest> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _queue.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_queue.Reader.TryRead(out var request))
            {
                yield return request;
            }
        }
    }

    public Task MarkAsCompletedAsync(Guid executionId, CancellationToken cancellationToken)
    {
        _statuses.AddOrUpdate(executionId, ExecutionStatus.Completed, (_, _) => ExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(Guid executionId, CancellationToken cancellationToken)
    {
        _statuses.AddOrUpdate(executionId, ExecutionStatus.Failed, (_, _) => ExecutionStatus.Failed);
        return Task.CompletedTask;
    }

    public ExecutionStatus? GetStatus(Guid executionId)
    {
        return _statuses.TryGetValue(executionId, out var status) ? status : null;
    }

    public enum ExecutionStatus
    {
        Pending,
        Completed,
        Failed
    }
}