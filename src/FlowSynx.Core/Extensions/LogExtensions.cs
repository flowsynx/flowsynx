using FlowSynx.Logging;
using FlowSynx.Commons;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Extensions;

internal static class LogExtensions
{
    public static LogLevel ToStandardLogLevel(this string logLevel)
    {
        var loggingLevel = string.IsNullOrEmpty(logLevel)
            ? LoggingLevel.Info
            : EnumUtils.GetEnumValueOrDefault<LoggingLevel>(logLevel)!.Value;

        var level = loggingLevel switch
        {
            LoggingLevel.Dbug => LogLevel.Debug,
            LoggingLevel.Info => LogLevel.Information,
            LoggingLevel.Warn => LogLevel.Warning,
            LoggingLevel.Fail => LogLevel.Error,
            LoggingLevel.Crit => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        return level;
    }

    public static LoggingLevel ToFlowSynxLogLevel(this LogLevel logLevel)
    {
        var level = logLevel switch
        {
            LogLevel.Debug => LoggingLevel.Dbug,
            LogLevel.Information => LoggingLevel.Info,
            LogLevel.Warning => LoggingLevel.Warn,
            LogLevel.Error => LoggingLevel.Fail,
            LogLevel.Critical => LoggingLevel.Crit,
            _ => LoggingLevel.Info,
        };

        return level;
    }
}