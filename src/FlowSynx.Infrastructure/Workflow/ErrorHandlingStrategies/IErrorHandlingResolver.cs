using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingResolver
{
    void Resolve(WorkflowDefinition definition);
}