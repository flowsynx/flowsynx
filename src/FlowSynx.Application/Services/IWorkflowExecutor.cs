using FlowSynx.Application.Features.Workflows.Command.Execute;
namespace FlowSynx.Application.Services;

public interface IWorkflowExecutor
{
    Task<object?> ExecuteAsync(string userId, Guid workflowId, CancellationToken cancellationToken);
}