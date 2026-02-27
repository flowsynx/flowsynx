namespace FlowSynx.Domain.Activities;

public class ExecutableComponent
{
    public ExecutableComponentType Type { get; set; } = ExecutableComponentType.Script;
    public ExecutableComponentLanguage Language { get; set; } = ExecutableComponentLanguage.JavaScript;
    public string Source { get; set; } = string.Empty;
    public string EntryPoint { get; set; } = string.Empty;
    public string Assembly { get; set; } = string.Empty;
    public ContainerSpec Container { get; set; } = new ContainerSpec();
    public HttpEndpoint Http { get; set; } = new HttpEndpoint();
    public GrpcEndpoint Grpc { get; set; } = new GrpcEndpoint();
    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
}