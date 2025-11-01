namespace FlowSynx.Application.Configuration;

public class SecretProviderConfiguration : Dictionary<string, string>
{
    public SecretProviderConfiguration() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}