namespace FlowSynx.Domain.Workflow;

public interface IWorkflowService
{
    Task<IReadOnlyCollection<WorkflowEntity>> All(string userId, CancellationToken cancellationToken);
    Task<WorkflowEntity?> Get(string userId, Guid workflowId, CancellationToken cancellationToken);
    Task<WorkflowEntity?> Get(string userId, string workflowName, CancellationToken cancellationToken);
    Task<bool> IsExist(string userId, string workflowName, CancellationToken cancellationToken);
    Task Add(WorkflowEntity workflowEntity, CancellationToken cancellationToken);
    Task Update(WorkflowEntity workflowEntity, CancellationToken cancellationToken);
    Task<bool> Delete(WorkflowEntity workflowEntity, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
    Task<int> GetActiveWorkflowsCountAsync(string userId, CancellationToken cancellationToken);
}