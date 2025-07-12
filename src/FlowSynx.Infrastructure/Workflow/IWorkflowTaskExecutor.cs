using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.Workflow.Parsers;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowTaskExecutor
{
    Task<object?> ExecuteAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task, 
        IExpressionParser parser,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken);
}