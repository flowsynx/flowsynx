namespace FlowSynx.Configuration.Server;

public class HttpsServerConfiguration
{
    public bool? Enabled { get; set; } = false;
    public int? Port { get; set; } = 6263;
    public HttpsServerCertificateConfiguration? Certificate { get; set; } = new();
}