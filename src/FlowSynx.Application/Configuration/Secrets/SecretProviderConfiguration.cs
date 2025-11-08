namespace FlowSynx.Application.Configuration.Secrets;

public class SecretProviderConfiguration : Dictionary<string, string>
{
    public SecretProviderConfiguration() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}