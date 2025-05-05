using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowValidator
{
    void Validate(WorkflowDefinition definition);
}