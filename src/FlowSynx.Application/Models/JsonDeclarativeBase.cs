namespace FlowSynx.Application.Models;

public abstract class JsonDeclarativeBase
{
    public string ApiVersion { get; set; } = "application/v1";
    public string Kind { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public object Spec { get; set; }
}