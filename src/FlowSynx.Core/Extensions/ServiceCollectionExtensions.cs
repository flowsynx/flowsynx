using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FlowSynx.Configuration;
using FlowSynx.Core.Behaviors;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.Core.Parers.Specifications;
using FlowSynx.Environment;
using FlowSynx.IO;
using FlowSynx.Parsers;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Extensions;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Storage.Amazon.S3;
using FlowSynx.Plugin.Storage.LocalFileSystem;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Storage.Azure.Blobs;
using FlowSynx.Plugin.Storage.Azure.Files;
using FlowSynx.Plugin.Storage.Extensions;
using FlowSynx.Plugin.Storage.Google.Cloud;
using FlowSynx.Plugin.Storage.Google.Drive;
using FlowSynx.Plugin.Storage.Memory;

namespace FlowSynx.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSynxCore(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(config => {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        services
            .AddEnvironmentManager()
            .AddEndpoint()
            .AddOperatingSystemInfo()
            .AddSystemClock()
            .AddSerialization()
            .AddFileSystem()
            .AddParsers()
            .AddCompressions()
            .AddPluginService()
            .AddStorageFilter()
            .AddMultiKeyCache<string, string, PluginInstance>()
            .AddScoped<IPluginInstanceParser, PluginInstanceParser>()
            .AddScoped<INamespaceParser, NamespaceParser>()
            .AddScoped<ISpecificationsParser, SpecificationsParser>();

        return services;
    }
    
    public static IServiceCollection AddFlowSynxConfiguration(this IServiceCollection services, string configurationPath)
    {
        services
            .AddSingleton(new ConfigurationPath { Path = configurationPath })
            .AddConfiguration();
        return services;
    }
    
    public static IServiceCollection AddFlowSynxPlugins(this IServiceCollection services)
    {
        services
            .RegisterPlugins()
            .AddPluginManager();
        return services;
    }

    public static IServiceCollection RegisterPlugins(this IServiceCollection services)
    {
        services.AddScoped<IPlugin, LocalFileSystemStorage>();
        services.AddScoped<IPlugin, MemoryStorage>();
        services.AddScoped<IPlugin, AzureFileStorage>();
        services.AddScoped<IPlugin, AzureBlobStorage>();
        services.AddScoped<IPlugin, GoogleCloudStorage>();
        services.AddScoped<IPlugin, AmazonS3Storage>();
        services.AddScoped<IPlugin, GoogleDriveStorage>();
        return services;
    }
}