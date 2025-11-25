using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.Workflow.Expressions;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowTaskExecutor
{
    Task<TaskOutput> ExecuteAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task, 
        IExpressionParser expressionParser,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken);
}