namespace FlowSynx.Infrastructure.Configuration.Server;

public class ServerConfiguration
{
    public HttpServerConfiguration? Http { get; set; }
    public HttpsServerConfiguration? Https { get; set; }
}