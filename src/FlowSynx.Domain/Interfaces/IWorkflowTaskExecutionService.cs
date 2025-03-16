using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Domain.Interfaces;

public interface IWorkflowTaskExecutionService
{
    Task<IReadOnlyCollection<WorkflowTaskExecutionEntity>> All(Guid workflowExecutionId, CancellationToken cancellationToken);
    Task<WorkflowTaskExecutionEntity?> Get(Guid workflowTaskExecutionId, CancellationToken cancellationToken);
    Task<WorkflowTaskExecutionEntity?> Get(Guid workflowExecutionId, string taskName, CancellationToken cancellationToken);
    Task Add(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken);
    Task Update(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken);
    Task<bool> Delete(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}