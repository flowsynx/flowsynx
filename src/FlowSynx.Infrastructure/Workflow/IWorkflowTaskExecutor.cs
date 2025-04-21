using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowTaskExecutor
{
    Task<object?> ExecuteAsync(string userId, WorkflowTask task, 
        IExpressionParser parser, CancellationToken cancellationToken);
}