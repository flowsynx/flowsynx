using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowValidator
{
    void Validate(WorkflowDefinition definition);
}