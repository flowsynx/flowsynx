using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Application.Services;

public interface IWorkflowValidator
{
    List<string> AllDependenciesExist(List<WorkflowTask> workflowTasks);
    WorkflowValidatorResult CheckCyclic(List<WorkflowTask> workflowTasks);
    bool HasDuplicateNames(List<WorkflowTask> workflowTasks);
}