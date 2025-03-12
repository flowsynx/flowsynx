namespace FlowSynx.Application.Configuration;

public class SecurityConfiguration
{
    public BasicAuthenticationConfiguration Basic { get; set; } = new BasicAuthenticationConfiguration();
    public OpenAuthenticationConfiguration OAuth2 { get; set; } = new OpenAuthenticationConfiguration();
}