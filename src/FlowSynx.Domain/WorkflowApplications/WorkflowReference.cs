namespace FlowSynx.Domain.WorkflowApplications;

public class WorkflowReference
{
    public string Reference { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Namespace { get; set; } = "default";

    public string? Condition { get; set; }

    public bool RunInParallel { get; set; } = false;

    public Dictionary<string, object> Params { get; set; } = new();
}