using FlowSynx.Application.Configuration.System.Cors;
using FlowSynx.Application.Configuration.System.HealthCheck;
using FlowSynx.Application.Configuration.System.OpenApi;
using FlowSynx.Application.Configuration.System.Server;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.HealthCheck;
using FlowSynx.Middleware;
using FlowSynx.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomException(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseCustomHeaders(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var versionService = serviceProvider.GetService<IVersion>();
        if (versionService == null)
            throw new ArgumentException(Localization.Get("UseCustomHeadersVersionServiceCouldNotBeInitialized"));

        var headers = new CustomHeadersToAddAndRemove();
        headers.HeadersToAdd.Add("flowsynx-version", versionService.Version.ToString());

        app.UseMiddleware<CustomHeadersMiddleware>(headers);
        return app;
    }

    public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var serializer = serviceProvider.GetRequiredService<IJsonSerializer>();
        var healthCheckConfiguration = serviceProvider.GetRequiredService<HealthCheckConfiguration>();

        if (!healthCheckConfiguration.Enabled)
            return app;

        app.UseEndpoints(endpoints => {
            if (serializer == null)
                throw new ArgumentException(Localization.Get("UseHealthCheckSerializerServiceCouldNotBeInitialized"));
                
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                },
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new HealthCheckResponse
                    {
                        Status = report.Status.ToString(),
                        HealthChecks = report.Entries.Select(x => new IndividualHealthCheckResponse
                        {
                            Component = x.Key,
                            Status = x.Value.Status.ToString(),
                            Description = x.Value.Description
                        }),
                        HealthCheckDuration = report.TotalDuration
                    };
                    await context.Response.WriteAsync(serializer.Serialize(response));
                }
            });
        });
        return app;
    }

    public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var openApiConfiguration = serviceProvider.GetRequiredService<OpenApiConfiguration>();

        if (!openApiConfiguration.Enabled)
            return app;

        app.UseSwagger(options =>
        {
            options.RouteTemplate = $"open-api/{{documentName}}/specifications.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "open-api";
            options.SwaggerEndpoint($"flowsynx/specifications.json", $"flowsynx");
        });

        return app;
    }

    public static IApplicationBuilder EnsureApplicationDatabaseCreated(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var initializers = serviceScope.ServiceProvider.GetServices<IDatabaseInitializer>();

        try
        {
            foreach (var initializer in initializers)
            {
                initializer.EnsureDatabaseCreatedAsync();
            }

            return app;
        }
        catch (Exception ex)
        {
            throw new FlowSynxException((int)ErrorCode.DatabaseCreation, 
                $"Error occurred while connecting the application database: {ex.Message}");
        }
    }

    public static IApplicationBuilder UseHttps(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var serverConfiguration = serviceProvider.GetService<ServerConfiguration>();
        if (serverConfiguration != null && serverConfiguration.Https?.Enabled == true)
        {
            app.UseHttpsRedirection();
        }

        return app;
    }

    public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var corsConfiguration = serviceProvider.GetService<CorsConfiguration>();
        if (corsConfiguration == null)
            throw new ArgumentException("Cors is not configured correctly.");

        var policyName = corsConfiguration.PolicyName ?? "DefaultCorsPolicy";
        app.UseCors(policyName);

        return app;
    }
}