namespace FlowSynx.Domain.Activities;

public class HttpEndpoint
{
    public string Url { get; set; } = string.Empty;
    public HttpEndpointMethod Method { get; set; } = HttpEndpointMethod.POST;
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public int Timeout { get; set; } = 30000;
    public bool Retry { get; set; } = true;
}