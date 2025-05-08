namespace FlowSynx.Domain.Workflow;

public interface IWorkflowTaskExecutionService
{
    Task<IReadOnlyCollection<WorkflowTaskExecutionEntity>> All(
        Guid workflowExecutionId, CancellationToken cancellationToken);

    Task<WorkflowTaskExecutionEntity?> Get(
        Guid workflowTaskExecutionId, CancellationToken cancellationToken);

    Task<WorkflowTaskExecutionEntity?> Get(
        Guid workflowId, Guid workflowExecutionId, string taskName, 
        CancellationToken cancellationToken);

    Task<WorkflowTaskExecutionEntity?> Get(
        Guid workflowId, Guid workflowExecutionId, Guid workflowTaskExecutionId,
        CancellationToken cancellationToken);

    Task Add(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, 
        CancellationToken cancellationToken);

    Task Update(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, 
        CancellationToken cancellationToken);

    Task<bool> Delete(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, 
        CancellationToken cancellationToken);

    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}