using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow;

public interface IRetryPolicyApplier
{
    void Apply(WorkflowDefinition definition);
}