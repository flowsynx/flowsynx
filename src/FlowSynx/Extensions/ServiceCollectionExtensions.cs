﻿using FlowSynx.HealthCheck;
using FlowSynx.Services;
using FlowSynx.Application.Services;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using FlowSynx.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication;
using FlowSynx.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FlowSynx.Application.Configuration;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;
using FlowSynx.Infrastructure.PluginHost;

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

    public static IServiceCollection AddEndpoint(this IServiceCollection services, IConfiguration configuration)
    {
        var endpointConfiguration = new EndpointConfiguration();
        configuration.GetSection("Endpoint").Bind(endpointConfiguration);
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

        var serviceProvider = services.BuildServiceProvider();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var logService = serviceProvider.GetRequiredService<ILoggerService>();
        var cancellationTokenSource = serviceProvider.GetRequiredService<CancellationTokenSource>();

        var cancellationToken = cancellationTokenSource.Token;
        var logLevel = loggerConfiguration.Level.ToLogLevel();

        services.AddLogging(c => c.ClearProviders());
        services.AddLogging(builder => builder.AddConsoleLogger(options =>
        {
            options.OutputTemplate = "[{level} | {timestamp}] Message=\"{message}\"";
            options.MinLevel = logLevel;
            options.CancellationToken = cancellationToken;
        }));

        services.AddLogging(builder => builder.AddDatabaseLogger(options =>
        {
            options.MinLevel = logLevel;
            options.CancellationToken = cancellationToken;
        }, httpContextAccessor, logService));

        return services;
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

        services
            .AddHealthChecks()
            .AddCheck<PluginConfigurationServiceHealthCheck>(name: Resources.AddHealthCheckerConfigurationService)
            .AddCheck<PluginsServiceHealthCheck>(name: Resources.AddHealthCheckerPluginService)
            .AddCheck<LogsServiceHealthCheck>(name: Resources.AddHealthCheckerLoggerService);

        return services;
    }

    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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

                if (!securityConfiguration.OAuth2.Enabled)
                {
                    c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic",
                        In = ParameterLocation.Header,
                        Description = "Input your username and password in the format: username:password"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "basic"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                }
                else
                {
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter your JWT Bearer token here"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                }
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
            using var scope = services.BuildServiceProvider().CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Initializing security");

            var securityConfiguration = new SecurityConfiguration();
            configuration.GetSection("Security").Bind(securityConfiguration);
            services.AddSingleton(securityConfiguration);

            // Integrate OAuth2/OpenID if enabled
            if (securityConfiguration.OAuth2.Enabled)
            {
                // Add authentication
                var authBuilder = services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                });

                // Add JWT Bearer authentication
                authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = securityConfiguration.OAuth2.Authority;
                    options.Audience = securityConfiguration.OAuth2.Audience;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = securityConfiguration.OAuth2.Issuer,
                        ValidAudience = securityConfiguration.OAuth2.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityConfiguration.OAuth2.Secret))
                    };
                });

                logger.LogInformation("OAuth2 authentication initialized.");
            }
            else
            {
                var userList = securityConfiguration.Basic.Users;
                if (userList == null || userList.GroupBy(u => u.Name).Any(g => g.Count() > 1))
                {
                    var errorMessage = new ErrorMessage((int)ErrorCode.SecurityBasicAuthenticationMustHaveUniqueNames, 
                        "Users must have unique names in BasicAuthentication configuration.");
                    throw new FlowSynxException(errorMessage);
                }

                // Add Basic Authentication only if OAuth2/OpenID is not enabled
                services.AddAuthentication("BasicAuthentication")
                    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

                logger.LogInformation("Basic authentication initialized.");
            }

            // Add authorization policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("JwtOrBasic", policy =>
                {
                    if (securityConfiguration.OAuth2.Enabled)
                    {
                        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    }
                    else
                    {
                        policy.AddAuthenticationSchemes("BasicAuthentication");
                    }

                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("User", policy => policy.RequireRole("User"));
                options.AddPolicy("Config", policy => policy.RequireRole("User"));
                options.AddPolicy("Logs", policy => policy.RequireRole("User"));
                options.AddPolicy("Plugins", policy => policy.RequireRole("User"));
                options.AddPolicy("Workflows", policy => policy.RequireRole("User"));

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
        using var scope = services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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
}