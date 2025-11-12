namespace FlowSynx.Application.Configuration.System.Server;

public class HttpsServerCertificateConfiguration
{
    public string Path { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
}