using Asp.Versioning;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using FlowSync.Core.Services;
using FlowSync.Services;
using FlowSync.Swagger;

namespace FlowSync.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAndConfigApiVersioning(this IServiceCollection services, ApiVersion version)
    {
        services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

        services.AddApiVersioning(
                options =>
                {
                    options.DefaultApiVersion = version;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                })
            .AddApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                })
            .EnableApiVersionBinding();

        return services;
    }

    public static IServiceCollection AddAndConfigSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }

    public static IServiceCollection AddLocation(this IServiceCollection services)
    {
        services.AddTransient<ILocation, FlowSyncLocation>();
        return services;
    }
}