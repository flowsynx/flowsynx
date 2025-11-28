using FlowSynx.Domain;
using FlowSynx.Domain.Log;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration.System.Logger;

public class LoggerConfiguration
{
    public bool Enabled { get; set; } = false;
    public string GlobalLogLevel { get; set; } = nameof(LogLevel.Information);
    public string[] DefaultProviders { get; set; } = Array.Empty<string>();
    public Dictionary<string, LoggerProviderConfiguration> Providers { get; set; } = new();

    public void ValidateLoggerProviders(ILogger logger)
    {
        if (!Enabled)
            return;

        var validProviders = Providers.Keys.ToList();
        logger.LogInformation("Configured Logger Providers: {Providers}", string.Join(", ", validProviders));

        var invalidProviders = DefaultProviders
            .Where(provider => !validProviders.Contains(provider))
            .ToList();

        if (invalidProviders.Any())
        {
            throw new FlowSynxException(
                (int)ErrorCode.LoggerConfigurationInvalidProviderName,
                $"Invalid default providers '{string.Join(", ", invalidProviders)}'. Available: {string.Join(", ", validProviders)}"
            );
        }

        if (!DefaultProviders.Any())
            logger.LogWarning("No default notification providers defined.");
    }
}