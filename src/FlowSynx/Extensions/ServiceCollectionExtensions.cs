using FlowSynx.HealthCheck;
using FlowSynx.Services;
using FlowSynx.Core.Services;
using FlowSynx.Environment;
using FlowSynx.Logging;

namespace FlowSynx.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocation(this IServiceCollection services)
    {
        services.AddTransient<ILocation, FlowSyncLocation>();
        return services;
    }

    public static IServiceCollection AddVersion(this IServiceCollection services)
    {
        services.AddTransient<IVersion, FlowSyncVersion>();
        return services;
    }

    public static IServiceCollection AddLoggingService(this IServiceCollection services, bool enable, AppLogLevel logLevel)
    {
        services.AddLogging(c => c.ClearProviders());

        if (!enable)
            return services;
        
        var level = logLevel switch
        {
            AppLogLevel.Dbug => LogLevel.Debug,
            AppLogLevel.Info => LogLevel.Information,
            AppLogLevel.Warn => LogLevel.Warning,
            AppLogLevel.Fail => LogLevel.Error,
            AppLogLevel.Crit => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        const string template = "[time={timestamp} | level={level} | machine={machine}] message=\"{message}\"";
        services.AddLogging(builder => builder.AddConsoleLogger(config =>
        {
            config.OutputTemplate = template;
            config.MinLevel = level;
        }));
        services.AddLogging(builder => builder.AddFileLogger(config =>
        {
            config.Path = @"D:\AminLog\";
            config.OutputTemplate = template;
            config.MinLevel = level;
        }));
        return services;
    }
    
    public static IServiceCollection AddHealthChecker(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<ConfigurationManagerHealthCheck>(name: Resources.AddHealthCheckerConfigurationRegistry)
            .AddCheck<PluginsManagerHealthCheck>(name: Resources.AddHealthCheckerPluginsRegistry);
        return services;
    }
}