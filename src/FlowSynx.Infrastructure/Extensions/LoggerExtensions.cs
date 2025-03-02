using FlowSynx.Core.Services;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Infrastructure.Logging.ConsoleLogger;
using FlowSynx.Infrastructure.Logging.DatabaseLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Extensions;

public static class LoggerExtensions
{
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddConsoleLogger(options =>
        {
            options.OutputTemplate = "[time={timestamp} | level={level}] message=\"{message}\"";
            options.MinLevel = LogLevel.Information;
        });
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        var loggerOptions = new ConsoleLoggerOptions();
        options(loggerOptions);

        builder.AddProvider(new ConsoleLoggerProvider(loggerOptions));

        return builder;
    }

    public static ILoggingBuilder AddDatabaseLogger(this ILoggingBuilder builder, Action<DatabaseLoggerOptions> options,
        IHttpContextAccessor httpContextAccessor, ILoggerService loggerService)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var loggerOptions = new DatabaseLoggerOptions();
        options(loggerOptions);

        builder.AddProvider(new DatabaseLoggerProvider(loggerOptions, httpContextAccessor, loggerService));

        return builder;
    }
}