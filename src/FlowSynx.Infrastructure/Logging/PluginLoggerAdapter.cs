using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public class PluginLoggerAdapter : IPluginLogger
{
    private readonly ILogger _logger;

    public PluginLoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }

    public void Log(PluginLoggerLevel level, string message)
    {
        LogLevel logLevel;
        switch (level)
        {
            case PluginLoggerLevel.Information:
                logLevel = LogLevel.Information;
                break;
            case PluginLoggerLevel.Error:
                logLevel = LogLevel.Error;
                break;
            case PluginLoggerLevel.Debug:
                logLevel = LogLevel.Debug;
                break;
            case PluginLoggerLevel.Warning:
                logLevel = LogLevel.Warning;
                break;
            default:
                logLevel = LogLevel.Information;
                break;
        };

        _logger.Log(logLevel, message);
    }
}
