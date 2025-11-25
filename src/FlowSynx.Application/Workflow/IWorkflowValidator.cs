using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowValidator
{
    Task ValidateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}