namespace FlowSynx.Core.Features.Workflow.Query;

public interface IWorkflowExecutor
{
    Task<Dictionary<string, object?>> ExecuteAsync(WorkflowExecutionDefinition executionDefinition, 
        CancellationToken cancellationToken);
}

public class WorkflowExecutionDefinition
{
    public WorkflowPipelines WorkflowPipelines { get; set; } = new();
    public WorkflowVariables WorkflowVariables { get; set; } = new();
    public int DegreeOfParallelism { get; set; } = 3;
}