namespace FlowSynx.Domain.Workflows;

public class ResourceConstraints
{
    public string Cpu { get; set; } = "100m";
    public string Memory { get; set; } = "128Mi";
    public string Storage { get; set; } = "1Gi";
    public int MaxParallelism { get; set; } = 5;
}