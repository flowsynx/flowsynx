namespace FlowSynx.Domain.Workflow;

public interface IWorkflowApprovalService
{
    Task AddAsync(WorkflowApprovalEntity approvalEntity, CancellationToken cancellationToken);
    Task<WorkflowApprovalEntity?> GetByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken);
    Task UpdateAsync(WorkflowApprovalEntity approvalEntity, CancellationToken cancellationToken);
}