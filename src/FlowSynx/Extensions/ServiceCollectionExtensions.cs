using FlowSynx.Application.Abstractions.Messaging;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Application.Tenancy;
using FlowSynx.Configuration.Database;
using FlowSynx.Configuration.OpenApi;
using FlowSynx.Configuration.Server;
using FlowSynx.Domain.Primitives;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Infrastructure.Persistence.Abstractions;
using FlowSynx.Infrastructure.Persistence.Postgres.Configuration;
using FlowSynx.Infrastructure.Persistence.Sqlite;
using FlowSynx.Infrastructure.Persistence.Sqlite.Configuration;
using FlowSynx.Infrastructure.Persistence.Sqlite.Services;
using FlowSynx.Infrastructure.Security.Cryptography;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Security;
using FlowSynx.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace FlowSynx.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultSqliteProvider = "SQLite";
    private const string DatabaseSectionName = "Databases";

    #region Simple registrations

    public static IServiceCollection AddFlowSynxCancellationTokenSource(this IServiceCollection services)
    {
        services.AddSingleton(new CancellationTokenSource());
        return services;
    }

    public static IServiceCollection AddFlowSynxVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, FlowSynxVersion>();
        return services;
    }

    public static IServiceCollection AddFlowSynxEventPublisher(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<IEventPublisher, SignalREventPublisher<WorkflowsHub>>();
        return services;
    }

    public static IServiceCollection AddFlowSynxServer(this IServiceCollection services)
    {
        services.AddScoped(provider =>
        {
            var tenantConfig = provider.GetRequiredService<IConfiguration>();
            return tenantConfig.BindSection<ServerConfiguration>("System:Server");
        });

        return services;
    }

    public static IServiceCollection AddFlowSynxUserService(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }

    public static IServiceCollection AddFlowSynxTenantService(this IServiceCollection services)
    {
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddSingleton<ITenantContext, TenantContextAccessor>();
        return services;
    }

    #endregion

    #region Logging

    public static void AddFlowSynxLoggingFilter(this ILoggingBuilder builder)
    {
        builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    }

    public static IServiceCollection AddFlowSynxLoggingServices(this IServiceCollection services)
    {
        services.AddLoggers()
                .AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddFlowSynxLoggingFilter();
                });
        return services;
    }
    #endregion

    #region Health checks

    public static IServiceCollection AddFlowSynxHealthChecker(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    #endregion

    #region OpenAPI (Swagger)

    public static IServiceCollection AddFlowSynxApiDocumentation(this IServiceCollection services)
    {
        try
        {
            services.AddScoped(provider =>
            {
                var tenantConfig = provider.GetRequiredService<IConfiguration>();
                return tenantConfig.BindSection<OpenApiConfiguration>("OpenApi");
            });

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
                c.OperationFilter<TenantHeaderOperationFilter>();

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

    internal class TenantHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<IOpenApiParameter>();
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-Id",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }
    }

    #endregion

    #region JSON options

    public static IServiceCollection AddHttpJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    #endregion

    #region Security

    public static IServiceCollection AddFlowSynxDataProtection(this IServiceCollection services)
    {
        services.AddScoped<IDataProtectionFactory, DataProtectionFactory>();
        return services;
    }

    public static IServiceCollection AddFlowSynxSecurity(this IServiceCollection services)
    {
        try
        {
            services.AddScoped<IAuthenticationProvider, NoneAuthenticationProvider>();
            services.AddScoped<IAuthenticationProvider, BasicAuthenticationProvider>();
            services.AddScoped<IAuthenticationProvider, JwtTokenAuthenticationProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddAuthentication("Dynamic")
                .AddScheme<AuthenticationSchemeOptions, DynamicAuthHandler>(
                    "Dynamic", null);

            services.AddAuthorization(options =>
            {
                options.AddPermissionPolicies();
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

    public static IServiceCollection ParseFlowSynxArguments(this IServiceCollection services, string[] args)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var hasStartArgument = args.Contains("--start");
        if (!hasStartArgument)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, "The '--start' argument is required.");
            logger.LogError(errorMessage.ToString());
            Task.Delay(500).Wait();
            Environment.Exit(1);
        }

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

    #endregion

    #region Persistence

    public static IServiceCollection AddFlowSynxPersistence(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var tenantConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var dbConfig = LoadDatabaseConfiguration(tenantConfig);
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

    private static DatabaseConfiguration LoadDatabaseConfiguration(IConfiguration tenantConfig)
    {
        var configuration = tenantConfig;
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