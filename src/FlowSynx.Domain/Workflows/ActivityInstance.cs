using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.Workflows;

public class ActivityInstance
{
    public string Id { get; set; } = string.Empty;  // Local ID within the workflow
    public ActivityReference Activity { get; set; } = new();
    public Dictionary<string, object> Params { get; set; } = new();
    public ActivityConfiguration Configuration { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();  // IDs of other activities
    public string? Condition { get; set; }  // Expression
    public RetryPolicy? RetryPolicy { get; set; }  // Overrides activity's default
    public int? TimeoutMilliseconds { get; set; }  // Overrides activity's default
}