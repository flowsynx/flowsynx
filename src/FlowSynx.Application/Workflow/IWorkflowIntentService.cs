using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowIntentService
{
    Task<(WorkflowDefinition Definition, string RawJson, string PlanSummary)> SynthesizeAsync(
        string goal,
        string? capabilitiesJson,
        CancellationToken cancellationToken);
}