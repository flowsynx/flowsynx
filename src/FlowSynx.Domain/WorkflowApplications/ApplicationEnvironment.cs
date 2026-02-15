namespace FlowSynx.Domain.WorkflowApplications;

public class ApplicationEnvironment
{
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<SecretReference> Secrets { get; set; } = new();
    public List<ConfigMapReference> ConfigMaps { get; set; } = new();
    public Dictionary<string, object> SharedState { get; set; } = new();
}