namespace FlowSynx.Domain.Workflows;

public class ActivityConfiguration
{
    public string Mode { get; set; } = "default";
    public bool RunInParallel { get; set; } = false;
    public int Priority { get; set; } = 1;
}