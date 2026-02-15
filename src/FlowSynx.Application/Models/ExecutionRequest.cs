namespace FlowSynx.Application.Models;

public class ExecutionRequest
{
    public string ApiVersion { get; set; } = "execution/v1";
    public string Kind { get; set; } = "ExecutionRequest";
    public ExecutionMetadata Metadata { get; set; } = new ExecutionMetadata();
    public ExecutionSpec Spec { get; set; } = new ExecutionSpec();
}