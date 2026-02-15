namespace FlowSynx.Domain.Workflows;

public class WorkflowOutput
{
    public string Format { get; set; } = "json";
    public string Path { get; set; } = string.Empty;
    public List<OutputMapping> Variables { get; set; } = new();
}