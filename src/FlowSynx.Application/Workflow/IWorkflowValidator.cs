using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowValidator
{
    void Validate(WorkflowDefinition definition);
    //void EnsureAllDependenciesExist(IEnumerable<WorkflowTask> tasks);
    //void EnsureNoDuplicateTaskNames(IEnumerable<WorkflowTask> tasks);
    //void EnsureNoCyclicDependencies(IEnumerable<WorkflowTask> tasks);
    //void ValidateRetryPolicies(IEnumerable<WorkflowTask> tasks);
}