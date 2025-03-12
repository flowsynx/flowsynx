using FlowSynx.Domain.Entities.Workflow.Models;

namespace FlowSynx.Application.Services;

public interface IWorkflowValidator
{
    List<string> AllDependenciesExist(WorkflowTasks workflowTasks);
    WorkflowValidatorResult CheckCyclic(WorkflowTasks workflowTasks);
    bool HasDuplicateNames(WorkflowTasks workflowTasks);
}