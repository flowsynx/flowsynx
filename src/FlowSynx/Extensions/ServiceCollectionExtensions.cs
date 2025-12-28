using FlowSynx.Application.Services;
using FlowSynx.Domain.Primitives;
using FlowSynx.HealthCheck;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Configuration.Core.Database;
using FlowSynx.Infrastructure.Configuration.Core.Security;
using FlowSynx.Infrastructure.Configuration.Integrations.PluginRegistry;
using FlowSynx.Infrastructure.Configuration.System.Cors;
using FlowSynx.Infrastructure.Configuration.System.HealthCheck;
using FlowSynx.Infrastructure.Configuration.System.OpenApi;
using FlowSynx.Infrastructure.Configuration.System.RateLimiting;
using FlowSynx.Infrastructure.Configuration.System.Server;
using FlowSynx.Infrastructure.Encryption;
using FlowSynx.Localization;
using FlowSynx.Persistence.Sqlite.Extensions;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Security;
using FlowSynx.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
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
    private const string DatabaseSectionName = "Core:Databases";
    private const string WorkflowQueueSectionName = "System:Workflow:Queue";
    private const string EnsureWorkflowPluginsConfiguration = "System:Workflow:Execution:EnsureWorkflowPlugins";

    #region Simple registrations

    /// <summary>Registers a single CancellationTokenSource as a singleton.</summary>
    public static IServiceCollection AddCancellationTokenSource(this IServiceCollection services)
    {
        services.AddSingleton(new CancellationTokenSource());
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

    /// <summary>Bind and register server configuration.</summary>
    public static IServiceCollection AddServer(this IServiceCollection services, IConfiguration configuration)
    {
        var serverConfiguration = configuration.BindSection<ServerConfiguration>("System:Server");
        services.AddSingleton(serverConfiguration);
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
    #endregion

    #region Health checks

    /// <summary>Register health check configuration and checks if enabled.</summary>
    public static IServiceCollection AddHealthChecker(this IServiceCollection services, IConfiguration configuration)
    {
        var healthCheckConfiguration = configuration.BindSection<HealthCheckConfiguration>("System:HealthCheck");
        services.AddSingleton(healthCheckConfiguration);

        if (!healthCheckConfiguration.Enabled)
            return services;

        using var scope = services.BuildServiceProvider().CreateScope();
        services
            .AddHealthChecks()
            .AddCheck<LogsServiceHealthCheck>(name: FlowSynxResources.AddHealthCheckerLoggerService);

        return services;
    }

    #endregion

    #region OpenAPI (Swagger)

    /// <summary>Configure OpenAPI/Swagger when enabled in configuration.</summary>
    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            var openApiConfiguration = configuration.BindSection<OpenApiConfiguration>("System:OpenApi");
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

                c.AddSecurityRequirement(document =>
                {
                    var requirement = new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Basic", document)] = new List<string>(),
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                    };

                    return requirement;
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
        var pluginRegistryConfiguration = configuration.BindSection<PluginRegistryConfiguration>("Integrations:PluginRegistry");
        services.AddSingleton(pluginRegistryConfiguration);

        return services;
    }

    #endregion

    #region Security

    public static IServiceCollection AddEncryptionService(this IServiceCollection services, IConfiguration configuration)
    {
        var securityConfiguration = configuration.BindSection<SecurityConfiguration>("Core:Security");
        services.AddSingleton<IEncryptionService>(provider =>
        {
            return new EncryptionService(securityConfiguration.Encryption.Key);
        });

        return services;
    }

    /// <summary>Configures authentication providers, authorization policies, and encryption service.</summary>
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var securityConfiguration = configuration.BindSection<SecurityConfiguration>("Core:Security");
            services.AddSingleton(securityConfiguration);

            securityConfiguration.Authentication.ValidateDefaultScheme(logger);

            var providers = new List<IAuthenticationProvider>();

            if (!securityConfiguration.Authentication.Enabled)
            {
                providers.Add(new DisabledAuthenticationProvider());
            }
            else
            {
                if (securityConfiguration.Authentication.Basic.Enabled && securityConfiguration.Authentication.Basic.Users != null)
                    providers.Add(new BasicAuthenticationProvider());

                foreach (var jwt in securityConfiguration.Authentication.JwtProviders)
                    providers.Add(new JwtAuthenticationProvider(jwt));
            }

            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = securityConfiguration.Authentication.DefaultScheme ?? "Basic";
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
        configuration.GetSection("System:RateLimiting").Bind(rateLimitingConfiguration);
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

        var corsConfiguration = configuration.BindSection<CorsConfiguration>("System:Cors");
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
                //services.AddPostgresPersistenceLayer(activeConnection);
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
}