using FlowSynx.Application.Configuration.Cors;
using FlowSynx.Application.Configuration.Database;
using FlowSynx.Application.Configuration.Endpoint;
using FlowSynx.Application.Configuration.HealthCheck;
using FlowSynx.Application.Configuration.Logger;
using FlowSynx.Application.Configuration.OpenApi;
using FlowSynx.Application.Configuration.PluginRegistry;
using FlowSynx.Application.Configuration.RateLimiting;
using FlowSynx.Application.Configuration.Security;
using FlowSynx.Application.Configuration.WorkflowQueue;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Log;
using FlowSynx.HealthCheck;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Persistence.Logging.Sqlite.Contexts;
using FlowSynx.Persistence.Postgres.Extensions;
using FlowSynx.Persistence.Sqlite.Extensions;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Security;
using FlowSynx.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace FlowSynx.Extensions;

/// <summary>
/// Extension methods for configuring services used across the FlowSynx application.
/// Focused on clarity, consistency and small helper extraction to reduce duplication.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string DefaultSqliteProvider = "SQLite";
    private const string DatabaseSectionName = "Databases";
    private const string WorkflowQueueSectionName = "WorkflowQueue";

    #region Simple registrations

    /// <summary>Registers a single CancellationTokenSource as a singleton.</summary>
    public static IServiceCollection AddCancellationTokenSource(this IServiceCollection services)
    {
        services.AddSingleton(new CancellationTokenSource());
        return services;
    }

    /// <summary>Register plugin location provider.</summary>
    public static IServiceCollection AddPluginsPath(this IServiceCollection services)
    {
        services.AddSingleton<IPluginsLocation, PluginsLocation>();
        return services;
    }

    /// <summary>Register application version provider.</summary>
    public static IServiceCollection AddVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, FlowSynxVersion>();
        return services;
    }

    /// <summary>Register SignalR and the event publisher implementation.</summary>
    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<IEventPublisher, SignalREventPublisher<WorkflowsHub>>();
        return services;
    }

    /// <summary>Bind and register endpoint configuration.</summary>
    public static IServiceCollection AddEndpoint(this IServiceCollection services, IConfiguration configuration)
    {
        var endpointConfiguration = configuration.BindSection<EndpointConfiguration>("Endpoints");
        services.AddSingleton(endpointConfiguration);
        return services;
    }

    /// <summary>Register current user service (transient).</summary>
    public static IServiceCollection AddUserService(this IServiceCollection services)
    {
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        return services;
    }

    #endregion

    #region Logging

    /// <summary>Filter EF Core database command logs to warning or above.</summary>
    public static void AddLoggingFilter(this ILoggingBuilder builder)
    {
        builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    }

    /// <summary>
    /// Configure application logging from configuration, register console and database loggers and ensure the
    /// logging database exists.
    /// </summary>
    public static IServiceCollection AddLoggingService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loggerConfiguration = configuration.BindSection<LoggerConfiguration>("Logger");
        services.AddSingleton(loggerConfiguration);

        // NOTE: resolving services during startup is unavoidable here because database and http-context
        // dependent loggers are created. We scope the provider to keep this localized.
        using var scope = services.BuildServiceProvider().CreateScope();
        var provider = scope.ServiceProvider;

        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        var logService = provider.GetRequiredService<ILoggerService>();
        var cancellationTokenSource = provider.GetRequiredService<CancellationTokenSource>();

        var cancellationToken = cancellationTokenSource.Token;
        var logLevel = loggerConfiguration.Level.ToLogLevel();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(logLevel);
            builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

            builder.AddConsoleLogger(options =>
            {
                options.OutputTemplate = "{timestamp} [{level}] Message=\"{message}\"";
                options.MinLevel = logLevel;
                options.CancellationToken = cancellationToken;
            });

            builder.AddDatabaseLogger(options =>
            {
                options.MinLevel = LogLevel.Debug;
                options.CancellationToken = cancellationToken;
            }, httpContextAccessor, logService);
        });

        services.EnsureLogDatabaseCreated();

        return services;
    }

    private static void EnsureLogDatabaseCreated(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LoggerContext>();

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
        return logsLevel?.ToLowerInvariant() switch
        {
            "none" => LogLevel.None,
            "dbug" => LogLevel.Debug,
            "info" => LogLevel.Information,
            "warn" => LogLevel.Warning,
            "fail" => LogLevel.Error,
            "crit" => LogLevel.Critical,
            _ => LogLevel.Information,
        };
    }

    #endregion

    #region Health checks

    /// <summary>Register health check configuration and checks if enabled.</summary>
    public static IServiceCollection AddHealthChecker(this IServiceCollection services, IConfiguration configuration)
    {
        var healthCheckConfiguration = configuration.BindSection<HealthCheckConfiguration>("HealthCheck");
        services.AddSingleton(healthCheckConfiguration);

        if (!healthCheckConfiguration.Enabled)
            return services;

        using var scope = services.BuildServiceProvider().CreateScope();
        var localization = scope.ServiceProvider.GetRequiredService<ILocalization>();

        services
            .AddHealthChecks()
            .AddCheck<PluginConfigurationServiceHealthCheck>(name: localization.Get("AddHealthCheckerConfigurationService"))
            .AddCheck<PluginsServiceHealthCheck>(name: localization.Get("AddHealthCheckerPluginService"))
            .AddCheck<LogsServiceHealthCheck>(name: localization.Get("AddHealthCheckerLoggerService"));

        return services;
    }

    #endregion

    #region OpenAPI (Swagger)

    /// <summary>Configure OpenAPI/Swagger when enabled in configuration.</summary>
    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            var openApiConfiguration = configuration.BindSection<OpenApiConfiguration>("OpenApi");
            services.AddSingleton(openApiConfiguration);

            if (!openApiConfiguration.Enabled)
                return services;

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

    #endregion

    #region JSON options

    /// <summary>Configure default HTTP JSON serialization options.</summary>
    public static IServiceCollection AddHttpJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    #endregion

    #region Plugin manager

    public static IServiceCollection AddInfrastructurePluginManager(this IServiceCollection services, IConfiguration configuration)
    {
        var pluginRegistryConfiguration = configuration.BindSection<PluginRegistryConfiguration>("PluginRegistry");
        services.AddSingleton(pluginRegistryConfiguration);
        services.AddPluginManager();

        return services;
    }

    #endregion

    #region Security

    /// <summary>Configures authentication providers and authorization policies.</summary>
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var securityConfiguration = configuration.BindSection<SecurityConfiguration>("Security");
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

            services.AddAuthorization(options =>
            {
                // keep authorization roles explicit and grouped for readability
                void AddRolePolicy(string name, string role) => 
                    options.AddPolicy(name, policy => policy.RequireRole(role));

                AddRolePolicy("admin", "admin");
                AddRolePolicy("user", "user");
                AddRolePolicy("audits", "audits");
                AddRolePolicy("config", "config");
                AddRolePolicy("logs", "logs");
                AddRolePolicy("plugins", "plugins");
                AddRolePolicy("workflows", "workflows");
                AddRolePolicy("executions", "executions");
                AddRolePolicy("triggers", "triggers");

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

    #endregion

    #region Arguments / Version helpers

    /// <summary>Parse required startup arguments and exit the process with an error when missing.</summary>
    public static IServiceCollection ParseArguments(this IServiceCollection services, string[] args)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var hasStartArgument = args.Contains("--start");
        if (!hasStartArgument)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, "The '--start' argument is required.");
            logger.LogError(errorMessage.ToString());

            // short delay to help ensure message appears in console before exit
            Task.Delay(500).Wait();

            Environment.Exit(1);
        }

        return services;
    }

    /// <summary>Check for version flags and print the application version.</summary>
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

    #endregion

    #region Rate limiting

    /// <summary>Add rate limiting services.</summary>
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

    #endregion

    #region CORS

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var corsConfiguration = configuration.BindSection<CorsConfiguration>("Cors");
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

    #endregion

    #region Persistence

    /// <summary>Setup persistence configuration and register provider-specific persistence layers.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConfig = LoadDatabaseConfiguration(configuration);
        var activeConnection = dbConfig.GetActiveConnection();

        services.AddSingleton(dbConfig);
        services.AddSingleton(activeConnection);
        services.AddSingleton<IDatabaseProvider>(new DatabaseProvider(activeConnection.Provider));

        RegisterPersistenceLayer(services, activeConnection);

        return services;
    }

    private static void RegisterPersistenceLayer(IServiceCollection services, DatabaseConnection activeConnection)
    {
        switch (activeConnection.Provider.ToLowerInvariant())
        {
            case "postgres":
                services.AddPostgresPersistenceLayer(activeConnection);
                break;

            case "sqlite":
                services.AddSqlitePersistenceLayer(activeConnection);
                break;

            default:
                throw new InvalidOperationException($"Unsupported database provider '{activeConnection.Provider}'.");
        }
    }

    private static DatabaseConfiguration LoadDatabaseConfiguration(IConfiguration configuration)
    {
        var databasesSection = configuration.GetSection(DatabaseSectionName);

        if (!databasesSection.Exists() || !databasesSection.GetChildren().Any())
            return CreateDefaultDatabaseConfiguration();

        var config = new DatabaseConfiguration
        {
            Default = databasesSection.GetValue<string>("Default") ?? DefaultSqliteProvider
        };

        var connectionsSection = databasesSection.GetSection("Connections");
        if (!connectionsSection.Exists() || !connectionsSection.GetChildren().Any())
        {
            config.Connections[DefaultSqliteProvider] = CreateDefaultSqliteConnection();
            return config;
        }

        foreach (var connectionSection in connectionsSection.GetChildren())
        {
            var provider = connectionSection.GetValue<string>("Provider") ?? DefaultSqliteProvider;

            DatabaseConnection connection = provider.ToLowerInvariant() switch
            {
                "postgres" => connectionSection.Get<PostgreDatabaseConnection>()!,
                "sqlite" => connectionSection.Get<SqliteDatabaseConnection>()!,
                _ => throw new InvalidOperationException($"Unsupported provider: {provider}")
            };

            connection.Provider = provider;
            config.Connections[connectionSection.Key] = connection;
        }

        return config;
    }

    private static DatabaseConfiguration CreateDefaultDatabaseConfiguration() => new()
    {
        Default = DefaultSqliteProvider,
        Connections = new Dictionary<string, DatabaseConnection>
        {
            [DefaultSqliteProvider] = CreateDefaultSqliteConnection()
        }
    };

    private static SqliteDatabaseConnection CreateDefaultSqliteConnection() => new()
    {
        Provider = DefaultSqliteProvider,
        FilePath = "flowsynx.db"
    };

    #endregion

    #region Workflow Queue

    public static IServiceCollection AddWorkflowQueueService(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var provider = scope.ServiceProvider;

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var localization = provider.GetRequiredService<ILocalization>();
            var dbProvider = provider.GetRequiredService<IDatabaseProvider>();

            var config = configuration.BindSection<WorkflowQueueConfiguration>(WorkflowQueueSectionName);
            services.AddSingleton(config);

            var providerName = config.Provider?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(providerName))
                return RegisterInMemoryQueue(services, logger);

            return providerName switch
            {
                "durable" => RegisterDurableQueue(services, logger, dbProvider, localization, config),
                "inmemory" => RegisterInMemoryQueue(services, logger),
                _ => ThrowQueueProviderNotSupported(config, localization)
            };
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowQueueProviderInitializedError, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    private static IServiceCollection RegisterInMemoryQueue(IServiceCollection services, ILogger logger)
    {
        logger.LogInformation("Initializing In-Memory Workflow Queue...");
        return services.AddInMemoryWorkflowQueueService();
    }

    private static IServiceCollection RegisterDurableQueue(
        IServiceCollection services,
        ILogger logger,
        IDatabaseProvider dbProvider,
        ILocalization localization,
        WorkflowQueueConfiguration config)
    {
        var dbName = dbProvider.Name?.ToLowerInvariant();
        logger.LogInformation("Initializing Durable Workflow Queue (Database: {Database})", dbName);

        return dbName switch
        {
            "postgres" => services.AddPostgreDurableWorkflowQueueService(),
            "sqlite" => services.AddSqliteDurableWorkflowQueueService(),
            _ => ThrowQueueProviderNotSupported(config, localization)
        };
    }

    private static IServiceCollection ThrowQueueProviderNotSupported(WorkflowQueueConfiguration config, ILocalization localization)
    {
        throw new FlowSynxException(
            (int)ErrorCode.WorkflowQueueProviderNotSupported,
            localization.Get("WorkflowQueueProvider_NotSupported", config.Provider));
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Bind a configuration section to a configuration object and return it. Simplifies repeated
    /// pattern of creating config instances and binding sections.
    /// </summary>
    private static T BindSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }

    #endregion
}