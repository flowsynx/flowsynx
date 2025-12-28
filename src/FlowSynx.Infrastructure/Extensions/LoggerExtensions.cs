using FlowSynx.Application;
using FlowSynx.Infrastructure.Logging.DatabaseLogger;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Configuration;
using System.Globalization;

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
            ILogEntryRepository logEntryRepository,
            IHttpContextAccessor? accessor)
    {
        return config.Sink(
            new SqliteLoggerSink(
                logEntryRepository: logEntryRepository,
                httpContextAccessor: accessor,
                CultureInfo.InvariantCulture));
    }
}