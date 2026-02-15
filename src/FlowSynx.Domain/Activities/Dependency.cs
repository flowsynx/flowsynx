namespace FlowSynx.Domain.Activities;

public class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = "activity"; // "activity", "workflow", "application"
}