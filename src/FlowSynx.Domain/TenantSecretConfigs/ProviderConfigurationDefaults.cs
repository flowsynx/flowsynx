namespace FlowSynx.Domain.TenantSecretConfigs;

public static class ProviderConfigurationDefaults
{
    public static ProviderConfiguration Default
    {
        get
        {
            var settings = new Dictionary<string, string>
            {
                { "ApiEndpoint", "https://api.defaultsecrets.com" },
                { "Timeout", "30" } // Timeout in seconds
            };

            return new ProviderConfiguration(settings);
        }
    }
}