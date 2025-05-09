using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingResolver
{
    void Resolve(WorkflowDefinition definition);
}