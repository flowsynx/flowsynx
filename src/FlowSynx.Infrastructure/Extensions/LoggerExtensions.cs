using FlowSynx.Domain.Log;
using FlowSynx.Infrastructure.Logging.DatabaseLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;

namespace FlowSynx.Infrastructure.Extensions;

public static class LoggerExtensions
{
    public static Serilog.Events.LogEventLevel ToSerilogLevel(this string logLevel)
    {
        return Enum.TryParse<Serilog.Events.LogEventLevel>(logLevel, true, out var lvl)
             ? lvl
             : Serilog.Events.LogEventLevel.Information;
    }

    public static LogLevel ToLogsLevel(this string logLevel)
    {
        return logLevel switch
        {
            "None" => LogLevel.None,
            "Trace" => LogLevel.Trace,
            "Dbug" => LogLevel.Debug,
            "Info" => LogLevel.Information,
            "Warn" => LogLevel.Warning,
            "Fail" => LogLevel.Error,
            "Crit" => LogLevel.Critical,
            _ => LogLevel.Information,
        };
    }

    public static RollingInterval RollingIntervalFromString(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return RollingInterval.Infinite;

        return Enum.TryParse<RollingInterval>(value, true, out var interval)
            ? interval
            : RollingInterval.Day;
    }

    public static LoggerConfiguration EfCoreLogs(
            this LoggerSinkConfiguration config,
            ILoggerService loggerService,
            IHttpContextAccessor? accessor = null)
    {
        return config.Sink(
            new EfCoreLoggerSink(
                loggerService: loggerService,
                httpContextAccessor: accessor));
    }
}