namespace FlowSynx.Application.Configuration;

public class SecurityConfiguration
{
    public BasicAuthenticationConfiguration Basic { get; set; } = new();
    public OpenAuthenticationConfiguration OAuth2 { get; set; } = new();
}