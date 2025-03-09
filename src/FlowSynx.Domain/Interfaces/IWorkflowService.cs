using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Domain.Interfaces;

public interface IWorkflowService
{
    Task<IReadOnlyCollection<WorkflowDefination>> All(string userId, CancellationToken cancellationToken);
    Task<WorkflowDefination?> Get(string userId, Guid workflowId, CancellationToken cancellationToken);
    Task<WorkflowDefination?> Get(string userId, string workflowName, CancellationToken cancellationToken);
    Task<bool> IsExist(string userId, string workflowName, CancellationToken cancellationToken);
    Task Add(WorkflowDefination workflow, CancellationToken cancellationToken);
    Task Update(WorkflowDefination workflow, CancellationToken cancellationToken);
    Task<bool> Delete(WorkflowDefination workflow, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}