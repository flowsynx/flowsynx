using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Domain.Interfaces;

public interface IWorkflowValidator
{
    List<string> AllDependenciesExist(WorkflowTasks workflowTasks);
    WorkflowValidatorResult CheckCyclic(WorkflowTasks workflowTasks);
    bool HasDuplicateNames(WorkflowTasks workflowTasks);
}