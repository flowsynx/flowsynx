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

    Task<WorkflowApprovalEntity?> GetAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        Guid approvalId,
        CancellationToken cancellationToken);

    Task<WorkflowApprovalEntity?> GetByTaskNameAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        string taskName,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        WorkflowApprovalEntity approvalEntity, 
        CancellationToken cancellationToken);
}