namespace FlowSynx.Domain.Activities;

public class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DependencyType Type { get; set; } = DependencyType.Activity;
}