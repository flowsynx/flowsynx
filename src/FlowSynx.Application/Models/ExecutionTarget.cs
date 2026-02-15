namespace FlowSynx.Application.Models;

public class ExecutionTarget
{
    public string Type { get; set; } // "activity", "workflow", "workflowApplication"
    public string Name { get; set; }
    public string Namespace { get; set; } = "default";
    public string Version { get; set; } = "latest";
}