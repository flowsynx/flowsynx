using FlowSynx.HealthCheck;
using FlowSynx.Services;
using FlowSynx.Core.Services;
using FlowSynx.Environment;
using FlowSynx.Logging;
using Microsoft.OpenApi.Models;

namespace FlowSynx.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocation(this IServiceCollection services)
    {
        services.AddTransient<ILocation, FlowSynxLocation>();
        return services;
    }

    public static IServiceCollection AddVersion(this IServiceCollection services)
    {
        services.AddTransient<IVersion, FlowSynxVersion>();
        return services;
    }

    public static IServiceCollection AddLoggingService(this IServiceCollection services, bool enable = true, 
        LoggingLevel logLevel = LoggingLevel.Info, string? logFile = "")
    {
        services.AddLogging(c => c.ClearProviders());

        if (!enable)
            return services;
        
        var level = logLevel switch
        {
            LoggingLevel.Dbug => LogLevel.Debug,
            LoggingLevel.Info => LogLevel.Information,
            LoggingLevel.Warn => LogLevel.Warning,
            LoggingLevel.Fail => LogLevel.Error,
            LoggingLevel.Crit => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        const string template = "[time={timestamp} | level={level} | machine={machine}] message=\"{message}\"";
        services.AddLogging(builder => builder.AddConsoleLogger(config =>
        {
            config.OutputTemplate = template;
            config.MinLevel = level;
        }));

        if (!string.IsNullOrEmpty(logFile))
        {
            services.AddLogging(builder => builder.AddFileLogger(config =>
            {
                config.Path = logFile;
                config.OutputTemplate = template;
                config.MinLevel = level;
            }));
        }

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

    public static void AddOpenApi(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("flowsynx", new OpenApiInfo
            {
                Version = "flowsynx",
                Title = "Service Invocation",
                Description = "Using the service invocation API, your microservice can find and reliably communicate with " +
                              "other microservices in your system using standard protocols (gRPC or HTTP are currently supported).",
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
        });
    }

    public static void UseOpenApi(this IApplicationBuilder app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = $"open-api/{{documentName}}/specifications.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "open-api";
            options.SwaggerEndpoint($"flowsynx/specifications.json", $"flowsynx");
        });
    }
}