using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public class PluginLoggerAdapter : IPluginLogger
{
    private readonly ILogger _logger;

    public PluginLoggerAdapter(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public void Log(PluginLoggerLevel level, string message)
    {
        LogLevel logLevel = level switch
        {
            PluginLoggerLevel.Information => LogLevel.Information,
            PluginLoggerLevel.Error => LogLevel.Error,
            PluginLoggerLevel.Debug => LogLevel.Debug,
            PluginLoggerLevel.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, message);
    }
}
