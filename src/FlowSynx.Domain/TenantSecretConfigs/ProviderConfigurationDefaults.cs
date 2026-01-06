namespace FlowSynx.Domain.TenantSecretConfigs;

public static class ProviderConfigurationDefaults
{
    public static ProviderConfiguration Default
    {
        get
        {
            var settings = new Dictionary<string, string>();
            return new ProviderConfiguration(settings);
        }
    }
}