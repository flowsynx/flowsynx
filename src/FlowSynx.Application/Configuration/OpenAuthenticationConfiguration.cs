namespace FlowSynx.Application.Configuration;

public class OpenAuthenticationConfiguration
{
    public bool Enabled { get; set; } = false;
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}