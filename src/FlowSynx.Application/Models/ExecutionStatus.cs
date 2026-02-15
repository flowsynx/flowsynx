namespace FlowSynx.Application.Models;

public class ExecutionStatus
{
    public string Phase { get; set; } = "pending";  // "pending", "running", "succeeded", "failed", "cancelled"
    public string Message { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Progress { get; set; } = 0; // 0-100
    public string Health { get; set; } = "healthy"; // "healthy", "degraded", "unhealthy"
}