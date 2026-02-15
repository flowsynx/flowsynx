namespace FlowSynx.Application.Models;

public class ExecutionResponse
{
    public string ApiVersion { get; set; } = "execution/v1";
    public string Kind { get; set; } = "ExecutionResponse";
    public ExecutionResponseMetadata Metadata { get; set; } = new ExecutionResponseMetadata();
    public ExecutionStatus Status { get; set; } = new ExecutionStatus();
    public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
    public List<ExecutionError> Errors { get; set; } = new List<ExecutionError>();
    public List<ExecutionLog> Logs { get; set; } = new List<ExecutionLog>();
    public List<ExecutionArtifact> Artifacts { get; set; } = new List<ExecutionArtifact>();
}