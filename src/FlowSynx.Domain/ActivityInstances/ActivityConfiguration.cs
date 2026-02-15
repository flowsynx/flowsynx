namespace FlowSynx.Domain.ActivityInstances;

public class ActivityConfiguration
{
    public string Operation { get; set; } = string.Empty;
    public string Mode { get; set; } = "default";
    public bool RunInParallel { get; set; } = false;
    public int Priority { get; set; } = 1;
}