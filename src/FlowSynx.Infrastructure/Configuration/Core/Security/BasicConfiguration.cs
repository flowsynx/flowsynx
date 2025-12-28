namespace FlowSynx.Infrastructure.Configuration.Core.Security;

public class BasicConfiguration
{
    public bool Enabled { get; set; } = true;
    public List<BasicAuthenticationConfiguration> Users { get; set; } = new();
}