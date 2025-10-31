using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration;

public class ResultStorageConfiguration
{
    public string DefaultProvider { get; set; } = "Local";
    public List<ResultStorageProviderConfiguration> Providers { get; set; } = new()
    {
        new ResultStorageProviderConfiguration
        {
            Name = "Local",
            Configuration = new Dictionary<string, string>()
        }
    };

    public void ValidateResultStorage(ILogger logger)
    {
        var validStorage = new List<string>();

        foreach (var provider in Providers)
        {
            validStorage.Add(provider.Name);
            logger.LogInformation("Storage provider '{ProviderName}' configured", provider.Name);
        }

        if (!string.IsNullOrEmpty(DefaultProvider))
        {
            if (!validStorage.Contains(DefaultProvider))
            {
                throw new FlowSynxException((int)ErrorCode.SecurityConfigurationInvalidScheme,
                    Localization.Get("SecurityConfiguration_InvalidScheme", DefaultProvider,
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