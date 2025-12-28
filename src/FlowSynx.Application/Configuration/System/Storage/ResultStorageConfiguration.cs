using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration.System.Storage;

public class ResultStorageConfiguration
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<ResultStorageProviderConfiguration> Providers { get; set; } = new();

    /// <summary>
    /// Validates available storage providers and confirms the configured default is registered.
    /// </summary>
    /// <param name="logger">Application logger used to record configuration details.</param>
    public void ValidateResultStorage(ILogger logger)
    {
        var validStorage = Providers
            .Select(provider =>
            {
                logger.LogInformation("Storage provider '{ProviderName}' configured", provider.Name);
                return provider.Name;
            })
            .ToList();

        if (!string.IsNullOrEmpty(DefaultProvider))
        {
            if (!validStorage.Contains(DefaultProvider))
            {
                throw new FlowSynxException((int)ErrorCode.SecurityConfigurationInvalidScheme,
                    Localizations.Localization.Get("SecurityConfiguration_InvalidScheme", DefaultProvider,
                    string.Join(", ", validStorage)));
            }

            logger.LogInformation("Default storage provider name set to: {ProviderName}", DefaultProvider);
        }
        else
        {
            logger.LogWarning("No default storage provider name is defined.");
        }
    }
}

public class ResultStorageProviderConfiguration
{
    public string Name { get; set; } = default!;
    public Dictionary<string, string> Configuration { get; set; } = new();
}