using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.Workflows;

public class ActivityInstance
{
    public string Id { get; set; } = string.Empty;

    public ActivityReference Activity { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();

    public ActivityConfiguration Configuration { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();

    public string? Condition { get; set; } // Execution condition

    public RetryPolicy RetryPolicy { get; set; } = new();

    public int TimeoutMilliseconds { get; set; } = 5000;
}