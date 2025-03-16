using FlowSynx.Application.Features.Workflows.Command.Execute;
namespace FlowSynx.Application.Services;

public interface IWorkflowExecutor
{
    Task ExecuteAsync(string userId, Guid workflowId, CancellationToken cancellationToken);
}