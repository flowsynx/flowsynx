namespace FlowSynx.Domain.WorkflowApplications;

public class SecretReference
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = "default";
    public List<string> Keys { get; set; } = new List<string>();
}