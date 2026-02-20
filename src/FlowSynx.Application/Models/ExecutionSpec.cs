namespace FlowSynx.Application.Models;

public class ExecutionSpec
{
    public ExecutionTarget Target { get; set; } = new ExecutionTarget();
    public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Environment { get; set; } = new Dictionary<string, object>();
    public int Timeout { get; set; } = 300000;
    public bool DryRun { get; set; } = false;
    public bool Validate { get; set; } = true;
}