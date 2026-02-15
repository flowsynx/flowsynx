namespace FlowSynx.Domain.Activities;

public class ActivitySpecification
{
    public string Description { get; set; } = string.Empty;
    public string Blueprint { get; set; } = string.Empty;
    public List<ParameterDefinition> Parameters { get; set; } = new List<ParameterDefinition>();
    public ExecutionProfile ExecutionProfile { get; set; } = new ExecutionProfile();
    public CompatibilityMatrix Compatibility { get; set; } = new CompatibilityMatrix();
    public FaultHandling FaultHandling { get; set; } = new FaultHandling();
    public ExecutableComponent Executable { get; set; } = new ExecutableComponent();
    public List<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();
    public List<string> Tags { get; set; } = new List<string>();
}