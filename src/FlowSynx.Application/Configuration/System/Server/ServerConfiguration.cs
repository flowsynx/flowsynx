namespace FlowSynx.Application.Configuration.System.Server;

public class ServerConfiguration
{
    public HttpServerConfiguration? Http { get; set; }
    public HttpsServerConfiguration? Https { get; set; }
}