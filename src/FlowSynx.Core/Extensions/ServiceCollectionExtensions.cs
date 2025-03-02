using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FlowSynx.Core.Behaviors;

namespace FlowSynx.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(config => {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });

        return services;
    }

    //public static IServiceCollection AddFlowSynxConfiguration(this IServiceCollection services, string configurationPath)
    //{
    //    services
    //        .AddSingleton(new ConfigurationPath { Path = configurationPath })
    //        .AddConfiguration();
    //    return services;
    //}

    //public static IServiceCollection AddFlowSynxConnectors(this IServiceCollection services)
    //{
    //    services
    //        .RegisterConnectors()
    //        .AddConnectorsManager();
    //    return services;
    //}

    //public static IServiceCollection RegisterConnectors(this IServiceCollection services)
    //{
    //    services.AddScoped<Connector, LocalFileSystemConnector>();
    //    services.AddScoped<Connector, MemoryConnector>();
    //    services.AddScoped<Connector, AzureFileConnector>();
    //    services.AddScoped<Connector, AzureBlobConnector>();
    //    services.AddScoped<Connector, AmazonS3Connector>();
    //    services.AddScoped<Connector, GoogleCloudConnector>();
    //    services.AddScoped<Connector, GoogleDriveConnector>();
    //    services.AddScoped<Connector, CsvConnector>();
    //    services.AddScoped<Connector, JsonConnector>();
    //    services.AddScoped<Connector, MySqlConnector>();
    //    return services;
    //}
}