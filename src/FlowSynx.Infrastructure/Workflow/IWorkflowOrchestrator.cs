using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowOrchestrator
{
    Task ExecuteWorkflowAsync(string userId, Guid executionId, 
        WorkflowDefinition definition, CancellationToken cancellationToken);
}