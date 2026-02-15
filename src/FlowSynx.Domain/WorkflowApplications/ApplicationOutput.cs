namespace FlowSynx.Domain.WorkflowApplications;

public class ApplicationOutput
{
    public string Format { get; set; } = "json";
    public string Path { get; set; } = string.Empty;
    public List<ArtifactDefinition> Artifacts { get; set; } = new();
}