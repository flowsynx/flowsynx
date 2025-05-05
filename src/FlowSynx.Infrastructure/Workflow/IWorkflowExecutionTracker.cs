using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowExecutionTracker
{
    Task<Guid> TrackWorkflowAsync(string userId, Guid workflowId, 
        CancellationToken cancellationToken);

    Task TrackTasksAsync(Guid workflowExecutionId, IEnumerable<WorkflowTask> tasks, 
        CancellationToken cancellationToken);

    Task UpdateTaskStatusAsync(Guid workflowExecutionId, string taskName, 
        WorkflowTaskExecutionStatus status, CancellationToken cancellationToken);

    Task UpdateWorkflowStatusAsync(string userId, Guid executionId, WorkflowExecutionStatus status, 
        CancellationToken cancellationToken);
}