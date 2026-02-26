namespace FlowSynx.Domain.Activities;

public class ActivitySpecification
{
    public string Description { get; set; } = string.Empty;  // Detailed description
    public List<ParameterDefinition> Params { get; set; } = new();
    public ExecutionProfile ExecutionProfile { get; set; } = new();
    public CompatibilityMatrix Compatibility { get; set; } = new();
    public FaultHandling FaultHandling { get; set; } = new();
    public ExecutableComponent Executable { get; set; } = new();
}