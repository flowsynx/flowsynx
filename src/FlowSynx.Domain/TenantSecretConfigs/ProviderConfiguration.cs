namespace FlowSynx.Domain.TenantSecretConfigs;

public record ProviderConfiguration(Dictionary<string, string> Settings)
{
    public static ProviderConfiguration Create(Dictionary<string, string> settings)
    {
        return new ProviderConfiguration(settings);
    }
};