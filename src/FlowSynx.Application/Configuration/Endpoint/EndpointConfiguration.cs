namespace FlowSynx.Application.Configuration.Endpoint;

public class EndpointConfiguration
{
    public HttpEndpointConfiguration? Http { get; set; }
    public HttpsEndpointConfiguration? Https { get; set; }
}

public class HttpEndpointConfiguration
{
    public int? Port { get; set; } = 6263;
}

public class HttpsEndpointConfiguration
{
    public bool? Enabled { get; set; } = false;
    public int? Port { get; set; } = 6263;
    public CertificateOptions? Certificate { get; set; } = new();
}

public class CertificateOptions
{
    public string Path { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
}