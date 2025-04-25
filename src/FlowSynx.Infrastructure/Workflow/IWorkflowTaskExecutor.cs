using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
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