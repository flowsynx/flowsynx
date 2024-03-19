using FlowSynx.Environment;
using FlowSynx.HealthCheck;
using FlowSynx.IO.Serialization;
using FlowSynx.Middleware;
using FlowSynx.Models;
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
            throw new ArgumentException(Resources.UseCustomHeadersVersionServiceCouldNotBeInitialized);

        var headers = new CustomHeadersToAddAndRemove();
        headers.HeadersToAdd.Add("flowsynx-version", versionService.Version);

        app.UseMiddleware<CustomHeadersMiddleware>(headers);
        return app;
    }

    public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints => {
            var serviceProvider = app.ApplicationServices;
            var serializer = serviceProvider.GetService<ISerializer>();
            if (serializer == null)
                throw new ArgumentException(Resources.UseHealthCheckSerializerServiceCouldNotBeInitialized);
                
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
                    context.Response.ContentType = serializer.ContentMineType;
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
}