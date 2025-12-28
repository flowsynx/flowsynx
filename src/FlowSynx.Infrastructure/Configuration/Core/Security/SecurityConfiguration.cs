namespace FlowSynx.Infrastructure.Configuration.Core.Security;

public class SecurityConfiguration
{
    public AuthenticationConfiguration Authentication { get; set; } = new();
    public EncryptionConfiguration Encryption { get; set; } = new();
}