using FlowSync.Core.Services;
using FlowSync.Services;
using FlowSync.Enums;
using Serilog;
using Serilog.Events;
using FlowSync.HealthCheck;

namespace FlowSync.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocation(this IServiceCollection services)
    {
        services.AddTransient<ILocation, FlowSyncLocation>();
        return services;
    }

    public static IServiceCollection AddLoggingService(this IServiceCollection services, bool enable, AppLogLevel logLevel)
    {
        var level = logLevel switch
        {
            AppLogLevel.All => LogEventLevel.Verbose,
            AppLogLevel.Debug => LogEventLevel.Debug,
            AppLogLevel.Error => LogEventLevel.Error,
            AppLogLevel.Fatal => LogEventLevel.Fatal,
            AppLogLevel.Information => LogEventLevel.Information,
            AppLogLevel.Warning => LogEventLevel.Warning,
            _ => LogEventLevel.Verbose
        };

        services.AddLogging(c => c.ClearProviders());

        var logger = new LoggerConfiguration()
            .WriteTo.Conditional(_ => enable,
                config => config.Console(restrictedToMinimumLevel: level, outputTemplate: "[time={Timestamp:HH:mm:ss} level={Level}] message=\"{Message}\"{NewLine}{Exception}"))
            .CreateLogger();

        services.AddSerilog(logger);
        return services;
    }

    public static IServiceCollection AddHealthChecker(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<ConfigurationManagerHealthCheck>(name: "Configuration Registry")
            .AddCheck<PluginsManagerHealthCheck>(name: "Plugins Registry");
        return services;
    }
}