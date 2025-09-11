namespace FlowSynx.Domain.Workflow;

public interface IWorkflowExecutionService
{
    Task<IReadOnlyCollection<WorkflowExecutionEntity>> All(string userId, 
        Guid workflowId, CancellationToken cancellationToken);

    Task<WorkflowExecutionEntity?> Get(string userId, Guid workflowId, 
        Guid workflowExecutionId, CancellationToken cancellationToken);

    Task<bool> IsExist(string userId, Guid workflowExecutionId, 
        CancellationToken cancellationToken);

    Task Add(WorkflowExecutionEntity workflowExecutionEntity, 
        CancellationToken cancellationToken);

    Task Update(WorkflowExecutionEntity workflowExecutionEntity, 
        CancellationToken cancellationToken);

    Task<bool> Delete(WorkflowExecutionEntity workflowExecutionEntity, 
        CancellationToken cancellationToken);

    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);

    Task<int> GetRunningWorkflowCountAsync(string userId, CancellationToken cancellationToken);

    Task<int> GetCompletedWorkflowsCountAsync(string userId, CancellationToken cancellationToken);

    Task<int> GetFailedWorkflowsCountAsync(string userId, CancellationToken cancellationToken);
}