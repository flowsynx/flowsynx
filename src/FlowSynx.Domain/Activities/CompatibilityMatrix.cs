namespace FlowSynx.Domain.Activities;

public class CompatibilityMatrix
{
    public string MinRuntimeVersion { get; set; } = string.Empty;
    public List<string> Platforms { get; set; } = new List<string>();
    public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
    public List<string> IncompatibleWith { get; set; } = new List<string>();
    public CompatibilityConstraints? Constraints { get; set; }
}