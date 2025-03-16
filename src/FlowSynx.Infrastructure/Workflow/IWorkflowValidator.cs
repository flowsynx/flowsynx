using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowValidator
{
    List<string> AllDependenciesExist(WorkflowTasks workflowTasks);
    WorkflowValidatorResult CheckCyclic(WorkflowTasks workflowTasks);
    bool HasDuplicateNames(WorkflowTasks workflowTasks);
}