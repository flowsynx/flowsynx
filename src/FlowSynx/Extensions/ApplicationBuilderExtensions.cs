using FlowSynx.HealthCheck;
using FlowSynx.Middleware;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FlowSynx.Domain.Primitives;
using FlowSynx.Application.Serializations;
using FlowSynx.Infrastructure.Persistence;
using FlowSynx.Infrastructure.Configuration.Server;
using FlowSynx.Infrastructure.Configuration.OpenApi;

namespace FlowSynx.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomException(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseTenantLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantLoggingMiddleware>();
    }

    public static IApplicationBuilder UseTenantCors(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantCorsMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseTenants(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseTenantRateLimiting(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantRateLimitingMiddleware>();
        return app;
    }


    public static IApplicationBuilder UseCustomHeaders(this IApplicationBuilder app)
    {
        // Inject IVersion via middleware instead of locating from ApplicationServices
        app.UseMiddleware<VersionHeaderMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
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
                    // Resolve per-request instead of using ApplicationServices at startup
                    var serializer = context.RequestServices.GetRequiredService<ISerializer>();

                    context.Response.ContentType = "application/json";
                    var response = new HealthCheckResponse
                    {
                        Status = report.Status.ToString(),
                        HealthCheckDuration = report.TotalDuration
                    };
                    await context.Response.WriteAsync(serializer.Serialize(response));
                }
            });
        });

        return app;
    }

    public static IApplicationBuilder UseApiDocumentation(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var openApiConfiguration = serviceScope.ServiceProvider.GetRequiredService<OpenApiConfiguration>();

        if (!openApiConfiguration.Enabled)
            return app;

        app.UseSwagger(options =>
        {
            options.RouteTemplate = $"open-api/{{documentName}}/specifications.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "open-api";
            options.SwaggerEndpoint($"flowsynx/specifications.json", $"FlowSynx API");
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapSwagger()
                .RequireAuthorization("admin");
        });

        return app;
    }

    public static async Task<IApplicationBuilder> EnsureApplicationDatabaseCreated(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var initializers = serviceScope.ServiceProvider.GetServices<IDatabaseInitializer>();

        try
        {
            foreach (var initializer in initializers)
            {
                await initializer.EnsureDatabaseCreatedAsync();
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
        using var scope = app.ApplicationServices.CreateScope();
        var serverConfiguration = scope.ServiceProvider.GetRequiredService<ServerConfiguration>();
        if (serverConfiguration.Https?.Enabled == true)
            app.UseHttpsRedirection();
        return app;
    }

    //public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app)
    //{
    //    using var scope = app.ApplicationServices.CreateScope();
    //    var corsConfiguration = scope.ServiceProvider.GetRequiredService<CorsConfiguration>();
    //    if (corsConfiguration == null)
    //        throw new ArgumentException("Cors is not configured correctly.");

    //    var policyName = corsConfiguration.PolicyName ?? "DefaultCorsPolicy";
    //    app.UseCors(policyName);

    //    return app;
    //}
}