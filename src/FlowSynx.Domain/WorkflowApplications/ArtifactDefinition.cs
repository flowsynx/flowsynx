namespace FlowSynx.Domain.WorkflowApplications;

public class ArtifactDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // file, data, report
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}