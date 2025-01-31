using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Workflow.Query;

public class WorkflowRequest : IRequest<Result<object?>>
{
    public string WorkflowTemplate { get; set; }

    public WorkflowRequest(string workflowTemplate)
    {
        WorkflowTemplate = workflowTemplate;
    }
}

public class WorkflowTemplate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowVariables Variables { get; set; } = new WorkflowVariables();
    public required WorkflowPipelines Pipelines { get; set; }
    public WorkflowOutputs? Outputs { get; set; }
}

public class WorkflowVariables: Dictionary<string, object>
{

}

public class WorkflowPipelines: List<WorkflowTask>
{

}

public class WorkflowTask
{
    public WorkflowTask(string name)
    {
        Name = name;
        Options = new ConnectorOptions();
        Dependencies = new List<string>();
        Status = WorkflowTaskStatus.Pending;
    }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Type { get; set; }
    public required string Process { get; set; }
    public List<string> Dependencies { get; set; }
    public ConnectorOptions? Options { get; set; }
    public WorkflowTaskStatus Status { get; set; }
}

public class WorkflowOutputs: Dictionary<string, object>
{

}

public class WorkflowOutputStep
{
    public string? Description { get; set; }
    public required string Value { get; set; }
}