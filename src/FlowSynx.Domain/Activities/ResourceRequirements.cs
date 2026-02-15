namespace FlowSynx.Domain.Activities;

public class ResourceRequirements
{
    public Dictionary<string, string> Requests { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Limits { get; set; } = new Dictionary<string, string>();
}