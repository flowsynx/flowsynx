namespace FlowSynx.Application.Configuration.Core.Secrets;

public class SecretProviderConfiguration : Dictionary<string, string>
{
    public SecretProviderConfiguration() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}