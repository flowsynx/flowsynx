﻿using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using FlowSynx.Application.Configuration;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Persistence.SQLite.Contexts;
using FlowSynx.HealthCheck;
using FlowSynx.Services;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Security;
using FlowSynx.Application.Localizations;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FlowSynx.Persistence.Postgres.Extensions;
using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Hubs;

namespace FlowSynx.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCancellationTokenSource(this IServiceCollection services)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        services.AddSingleton(cancellationTokenSource);
        return services;
    }

    public static IServiceCollection AddPluginsPath(this IServiceCollection services)
    {
        services.AddSingleton<IPluginsLocation, PluginsLocation>();
        return services;
    }

    public static IServiceCollection AddVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, FlowSynxVersion>();
        return services;
    }

    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<IEventPublisher, SignalREventPublisher<WorkflowsHub>>();
        return services;
    }

    public static IServiceCollection AddEndpoint(this IServiceCollection services, IConfiguration configuration)
    {
        var endpointConfiguration = new EndpointConfiguration();
        configuration.GetSection("Endpoints").Bind(endpointConfiguration);
        services.AddSingleton(endpointConfiguration);
        return services;
    }

    public static IServiceCollection AddUserService(this IServiceCollection services)
    {
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        return services;
    }

    public static IServiceCollection AddLoggingService(this IServiceCollection services, IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration();
        configuration.GetSection("Logger").Bind(loggerConfiguration);
        services.AddSingleton(loggerConfiguration);

        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var httpContextAccessor = serviceProviderScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var logService = serviceProviderScope.ServiceProvider.GetRequiredService<ILoggerService>();
        var cancellationTokenSource = serviceProviderScope.ServiceProvider.GetRequiredService<CancellationTokenSource>();

        var cancellationToken = cancellationTokenSource.Token;
        var logLevel = loggerConfiguration.Level.ToLogLevel();

        services.AddLogging(c => c.ClearProviders());
        services.AddLogging(builder => builder.AddConsoleLogger(options =>
        {
            options.OutputTemplate = "{timestamp} [{level}] Message=\"{message}\"";
            options.MinLevel = logLevel;
            options.CancellationToken = cancellationToken;
        }));

        services.EnsureLogDatabaseCreated();
        services.AddLogging(builder => builder.AddDatabaseLogger(options =>
        {
            options.MinLevel = LogLevel.Debug;
            options.CancellationToken = cancellationToken;
        }, httpContextAccessor, logService));

        return services;
    }

    private static void EnsureLogDatabaseCreated(this IServiceCollection services)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var context = serviceProviderScope.ServiceProvider.GetRequiredService<LoggerContext>();

        try
        {
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            throw new FlowSynxException((int)ErrorCode.LoggerCreation, $"Error occurred while creating the logger: {ex.Message}");
        }
    }

    private static LogLevel ToLogLevel(this string logsLevel)
    {
        var level = logsLevel.ToLower() switch
        {
            "none" => LogLevel.None,
            "dbug" => LogLevel.Debug,
            "info" => LogLevel.Information,
            "warn" => LogLevel.Warning,
            "fail" => LogLevel.Error,
            "crit" => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        return level;
    }

    public static IServiceCollection AddHealthChecker(this IServiceCollection services, IConfiguration configuration)
    {
        var healthCheckConfiguration = new HealthCheckConfiguration();
        configuration.GetSection("HealthCheck").Bind(healthCheckConfiguration);
        services.AddSingleton(healthCheckConfiguration);

        if (!healthCheckConfiguration.Enabled)
            return services;

        var serviceProvider = services.BuildServiceProvider();
        var localization = serviceProvider.GetRequiredService<ILocalization>();

        services
            .AddHealthChecks()
            .AddCheck<PluginConfigurationServiceHealthCheck>(name: localization.Get("AddHealthCheckerConfigurationService"))
            .AddCheck<PluginsServiceHealthCheck>(name: localization.Get("AddHealthCheckerPluginService"))
            .AddCheck<LogsServiceHealthCheck>(name: localization.Get("AddHealthCheckerLoggerService"));

        return services;
    }

    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            var openApiConfiguration = new OpenApiConfiguration();
            configuration.GetSection("OpenApi").Bind(openApiConfiguration);
            services.AddSingleton(openApiConfiguration);

            if (!openApiConfiguration.Enabled)
                return services;

            var serviceProvider = services.BuildServiceProvider();
            var securityConfiguration = serviceProvider.GetRequiredService<SecurityConfiguration>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("flowsynx", new OpenApiInfo
                {
                    Version = "flowsynx",
                    Title = "Service Invocation",
                    Description = "Using the service invocation API to find out how to communicate with FlowSynx API.",
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    Description = "Basic Authentication using username and password."
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" }
                        },
                        new List<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new List<string>()
                    }
                });
            });

            return services;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationOpenApiService, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public static IServiceCollection AddHttpJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    public static IServiceCollection AddInfrastructurePluginManager(this IServiceCollection services, IConfiguration configuration)
    {
        var pluginRegistryConfiguration = new PluginRegistryConfiguration();
        configuration.GetSection("PluginRegistry").Bind(pluginRegistryConfiguration);
        services.AddSingleton(pluginRegistryConfiguration);
        services.AddPluginManager();

        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
            var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var securityConfiguration = new SecurityConfiguration();
            configuration.GetSection("Security").Bind(securityConfiguration);
            services.AddSingleton(securityConfiguration);

            securityConfiguration.ValidateDefaultScheme(logger);

            var providers = new List<IAuthenticationProvider>();

            if (securityConfiguration.EnableBasic && securityConfiguration.BasicUsers != null)
                providers.Add(new BasicAuthenticationProvider());

            foreach (var jwt in securityConfiguration.JwtProviders)
                providers.Add(new JwtAuthenticationProvider(jwt));

            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = securityConfiguration.DefaultScheme ?? "Basic";
            });

            foreach (var provider in providers)
                provider.Configure(authBuilder);

            // Add authorization policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("admin", policy => policy.RequireRole("admin"));
                options.AddPolicy("user", policy => policy.RequireRole("user"));
                options.AddPolicy("audits", policy => policy.RequireRole("audits"));
                options.AddPolicy("config", policy => policy.RequireRole("config"));
                options.AddPolicy("logs", policy => policy.RequireRole("logs"));
                options.AddPolicy("plugins", policy => policy.RequireRole("plugins"));
                options.AddPolicy("workflows", policy => policy.RequireRole("workflows"));
                options.AddPolicy("executions", policy => policy.RequireRole("executions"));
                options.AddPolicy("triggers", policy => policy.RequireRole("triggers"));

                logger.LogInformation("Authorization initialized.");
            });

            return services;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.SecurityInitializedError, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public static IServiceCollection ParseArguments(this IServiceCollection services, string[] args)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        bool hasStartArgument = args.Contains("--start");
        if (!hasStartArgument)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, "The '--start' argument is required.");
            logger.LogError(errorMessage.ToString());

            // if the console closes immediately, the output may not be visible.
            // So, added await Task.Delay(500) here;
            Task.Delay(500).Wait();

            Environment.Exit(1);
        }

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingConfiguration = new RateLimitingConfiguration();
        configuration.GetSection("RateLimiting").Bind(rateLimitingConfiguration);
        services.AddSingleton(rateLimitingConfiguration);

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("Fixed", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitingConfiguration.WindowSeconds);
                limiterOptions.PermitLimit = rateLimitingConfiguration.PermitLimit;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitingConfiguration.QueueLimit;
            });
        });

        return services;
    }

    public static bool HandleVersionFlag(this string[] args)
    {
        if (args.Any(arg => arg.Equals("--version", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("-v", StringComparison.OrdinalIgnoreCase)))
        {
            var version = FlowSynxVersion.GetApplicationVersion();
            Console.WriteLine($"FlowSynx Version: {version}");
            return true;
        }

        return false;
    }

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var corsConfiguration = new CorsConfiguration();
        configuration.GetSection("Cors").Bind(corsConfiguration);
        services.AddSingleton(corsConfiguration);

        var allowedOrigins = corsConfiguration.AllowedOrigins?.ToArray() ?? Array.Empty<string>();
        var allowCredentials = corsConfiguration.AllowCredentials;
        var policyName = corsConfiguration.PolicyName ?? "DefaultCorsPolicy";

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policyBuilder =>
            {
                if (allowedOrigins.Contains("*"))
                {
                    if (allowCredentials)
                    {
                        throw new InvalidOperationException("CORS configuration error: AllowCredentials cannot be used with wildcard origin '*'.");
                    }

                    policyBuilder.AllowAnyOrigin()
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                }
                else
                {
                    policyBuilder.WithOrigins(allowedOrigins)
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();

                    if (allowCredentials)
                    {
                        policyBuilder.AllowCredentials();
                    }
                }
            });
        });

        logger.LogInformation("Cors Initialized.");

        return services;
    }

    public static IServiceCollection AddWorkflowQueueService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        try
        {
            using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
            var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var localization = serviceProviderScope.ServiceProvider.GetRequiredService<ILocalization>();

            var workflowQueueConfiguration = new WorkflowQueueConfiguration();
            configuration.GetSection("WorkflowQueue").Bind(workflowQueueConfiguration);
            services.AddSingleton(workflowQueueConfiguration);

            if (string.IsNullOrEmpty(workflowQueueConfiguration.Provider))
                return services.AddInMemoryWorkflowQueueService();

            switch (workflowQueueConfiguration.Provider.ToLowerInvariant())
            {
                case "durable":
                    logger.LogInformation("Initializing Durable Workflow Queue");
                    return services.AddDurableWorkflowQueueService();
                case "inmemory":
                    logger.LogInformation("Initializing InMemory Workflow Queue");
                    return services.AddInMemoryWorkflowQueueService();
                default:
                    throw new FlowSynxException((int)ErrorCode.WorkflowQueueProviderNotSupported,
                        localization.Get("WorkflowQueueProvider_NotSupported", workflowQueueConfiguration.Provider));
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowQueueProviderInitializedError, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }
}