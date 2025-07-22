namespace FlowSynx.Domain.Workflow;

public interface IWorkflowApprovalService
{
    Task AddAsync(
        WorkflowApprovalEntity approvalEntity, 
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkflowApprovalEntity>> GetPendingApprovalsAsync(
        string userId, 
        Guid workflowId, 
        Guid executionId, 
        CancellationToken cancellationToken);

    Task<WorkflowApprovalEntity?> GetByExecutionIdAsync(
        Guid executionId, 
        CancellationToken cancellationToken);

    Task UpdateAsync(
        WorkflowApprovalEntity approvalEntity, 
        CancellationToken cancellationToken);
}