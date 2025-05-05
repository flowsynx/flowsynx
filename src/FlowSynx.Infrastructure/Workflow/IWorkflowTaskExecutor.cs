using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.Workflow.Parsers;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowTaskExecutor
{
    Task<object?> ExecuteAsync(
        string userId, 
        WorkflowTask task, 
        IExpressionParser parser,
        CancellationToken cancellationToken);
}