using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration.Core.Secrets;

public class SecretConfiguration
{
    public bool Enabled { get; set; } = false;
    public string DefaultProvider { get; set; } = "Infisical";
    public Dictionary<string, SecretProviderConfiguration> Providers { get; set; } = new();

    public void ValidateSecretProviders(ILogger logger)
    {
        if (!Enabled)
            return;

        var validProviders = Providers.Select(p =>
        {
            logger.LogInformation("Secret provider '{ProviderName}' configured", p.Key);
            return p.Key;
        }).ToList();

        if (!string.IsNullOrEmpty(DefaultProvider))
        {
            if (!validProviders.Contains(DefaultProvider))
            {
                throw new FlowSynxException((int)ErrorCode.SecretConfigurationInvalidProviderName,
                    Localizations.Localization.Get("SecretConfiguration_InvalidProvider", DefaultProvider,
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