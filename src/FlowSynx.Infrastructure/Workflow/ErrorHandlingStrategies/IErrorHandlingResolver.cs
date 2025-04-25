using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingResolver
{
    void Resolve(WorkflowDefinition definition);
}