using Serilog;

namespace FlowSynx.Infrastructure.Logging.Extensions;

public static class LoggerExtensions
{
    public static Serilog.Events.LogEventLevel ToSerilogLevel(this string logLevel)
    {
        return Enum.TryParse<Serilog.Events.LogEventLevel>(logLevel, true, out var lvl)
             ? lvl
             : Serilog.Events.LogEventLevel.Information;
    }

    public static RollingInterval RollingIntervalFromString(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return RollingInterval.Infinite;

        return Enum.TryParse<RollingInterval>(value, true, out var interval)
            ? interval
            : RollingInterval.Day;
    }

    public static Serilog.Events.LogEventLevel GetLogEventLevel(this string level) => level.ToUpper() switch
    {
        "VERBOSE" => Serilog.Events.LogEventLevel.Verbose,
        "DEBUG" => Serilog.Events.LogEventLevel.Debug,
        "INFORMATION" => Serilog.Events.LogEventLevel.Information,
        "WARNING" => Serilog.Events.LogEventLevel.Warning,
        "ERROR" => Serilog.Events.LogEventLevel.Error,
        "FATAL" => Serilog.Events.LogEventLevel.Fatal,
        _ => Serilog.Events.LogEventLevel.Information
    };
}