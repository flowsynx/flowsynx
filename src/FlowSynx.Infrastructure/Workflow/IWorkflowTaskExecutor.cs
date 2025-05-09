using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.Workflow.Parsers;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowTaskExecutor
{
    Task<object?> ExecuteAsync(
        string userId, 
        Guid workflowId,
        Guid workflowExecutionId,
        WorkflowTask task, 
        IExpressionParser parser,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken);
}