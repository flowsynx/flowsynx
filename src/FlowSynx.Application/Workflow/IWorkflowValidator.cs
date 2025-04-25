using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowValidator
{
    void Validate(WorkflowDefinition definition);
}