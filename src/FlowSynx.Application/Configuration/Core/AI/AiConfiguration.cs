using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration.Core.AI;

public class AiConfiguration
{
    public bool Enabled { get; set; } = false;
    public string DefaultProvider { get; set; } = "OpenAI";
    public Dictionary<string, AiProviderConfiguration> Providers { get; set; } = new();

    public void ValidateAiProviders(ILogger logger)
    {
        if (!Enabled)
            return;

        var validProviders = Providers.Select(p =>
        {
            logger.LogInformation("AI provider '{ProviderName}' configured", p.Key);
            return p.Key;
        }).ToList();

        if (!string.IsNullOrEmpty(DefaultProvider))
        {
            if (!validProviders.Contains(DefaultProvider))
            {
                throw new FlowSynxException((int)ErrorCode.AIConfigurationInvalidProviderName,
                    Localizations.Localization.Get("AIConfiguration_InvalidProvider", DefaultProvider,
                    string.Join(", ", validProviders)));
            }

            logger.LogInformation("Default AI provider name set to: {ProviderName}", DefaultProvider);
        }
        else
        {
            logger.LogWarning("No default AI provider name is defined.");
        }
    }
}