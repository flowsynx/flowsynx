namespace FlowSynx.Application.Features.Metrics.Requests;

public class SummaryResult
{
    public int ActiveWorkflows { get; set; }
    public int RunningTasks { get; set; }
    public int CompletedToday { get; set; }
    public int FailedWorkflows { get; set; }
}