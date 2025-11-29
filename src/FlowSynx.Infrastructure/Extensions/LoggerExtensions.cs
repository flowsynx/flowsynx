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

    public static RollingInterval RollingIntervalFromString(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return RollingInterval.Infinite;

        return Enum.TryParse<RollingInterval>(value, true, out var interval)
            ? interval
            : RollingInterval.Day;
    }

    public static LoggerConfiguration SqliteLogs(
            this LoggerSinkConfiguration config,
            ILoggerService loggerService,
            IHttpContextAccessor? accessor)
    {
        return config.Sink(
            new SqliteLoggerSink(
                loggerService: loggerService,
                httpContextAccessor: accessor));
    }
}