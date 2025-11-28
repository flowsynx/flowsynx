//using FlowSynx.Domain.Log;
//using FlowSynx.Infrastructure.Logging;
//using FlowSynx.Infrastructure.Logging.ConsoleLogger;
//using FlowSynx.Infrastructure.Logging.DatabaseLogger;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;

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

////public static class LoggerExtensions
////{
////    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder)
////    {
////        ArgumentNullException.ThrowIfNull(builder);

////        builder.AddConsoleLogger(options =>
////        {
////            options.OutputTemplate = "[time={timestamp} | level={level}] message=\"{message}\"";
////            options.MinLevel = LogLevel.Information;
////        });
////        return builder;
////    }

////    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> options)
////    {
////        ArgumentNullException.ThrowIfNull(builder);
////        ArgumentNullException.ThrowIfNull(options);

////        var loggerOptions = new ConsoleLoggerOptions();
////        options(loggerOptions);

////        builder.AddProvider(new ConsoleLoggerProvider(loggerOptions));

////        return builder;
////    }

////    public static ILoggingBuilder AddDatabaseLogger(this ILoggingBuilder builder, Action<DatabaseLoggerOptions> options,
////        IHttpContextAccessor httpContextAccessor, ILoggerService loggerService)
////    {
////        ArgumentNullException.ThrowIfNull(builder);

////        var loggerOptions = new DatabaseLoggerOptions();
////        options(loggerOptions);

////        builder.AddProvider(new DatabaseLoggerProvider(loggerOptions, httpContextAccessor, loggerService));

////        return builder;
////    }
////}