namespace FlowSynx.Domain.Activities;

public class ExecutableComponent
{
    public string Type { get; set; } = "script"; // "assembly", "script", "container", "http", "grpc"
    public string Language { get; set; } = "javascript"; // "javascript", "python", "csharp", "powershell"
    public string Source { get; set; } = string.Empty;
    public string EntryPoint { get; set; } = string.Empty;
    public string Assembly { get; set; } = string.Empty;
    public ContainerSpec Container { get; set; } = new ContainerSpec();
    public HttpEndpoint Http { get; set; } = new HttpEndpoint();
    public GrpcEndpoint Grpc { get; set; } = new GrpcEndpoint();
    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
}