using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration;

public class SecretConfiguration
{
    public bool Enabled { get; set; } = false;
    public string DefaultProvider { get; set; } = "Infisical";
    public Dictionary<string, SecretProviderConfiguration> Providers { get; set; } = new();

    public void ValidateSecretProviders(ILogger logger)
    {
        if (!Enabled)
            return;

        var validProviders = new List<string>();

        foreach (var provider in Providers)
        {
            validProviders.Add(provider.Key);
            logger.LogInformation("Secret provider '{ProviderName}' configured", provider.Key);
        }

        if (!string.IsNullOrEmpty(DefaultProvider))
        {
            if (!validProviders.Contains(DefaultProvider))
            {
                throw new FlowSynxException((int)ErrorCode.SecretConfigurationInvalidProviderName,
                    Localization.Get("SecretConfiguration_InvalidProvider", DefaultProvider,
                    string.Join(", ", validProviders)));
            }

            logger.LogInformation("Default secret provider name set to: {ProviderName}", DefaultProvider);
        }
        else
        {
            logger.LogWarning("No default secret provider name is defined.");
        }
    }
}