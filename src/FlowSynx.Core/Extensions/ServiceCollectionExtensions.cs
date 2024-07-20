using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FlowSynx.Configuration;
using FlowSynx.Core.Behaviors;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Parers.Specifications;
using FlowSynx.Core.Storage;
using FlowSynx.Core.Storage.Filter;
using FlowSynx.Environment;
using FlowSynx.IO;
using FlowSynx.Parsers;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Storage;
using FlowSynx.Plugin.Storage.Azure.Files;
using FlowSynx.Plugin.Storage.LocalFileSystem;
using FlowSynx.Core.Storage.Check;
using FlowSynx.Core.Storage.Compress;
using FlowSynx.Core.Storage.Copy;
using FlowSynx.Core.Storage.Move;
using FlowSynx.Plugin.Storage.Amazon.S3;
using FlowSynx.Plugin.Storage.Azure.Blobs;
using FlowSynx.Plugin.Storage.Google.Cloud;
using FlowSynx.Plugin.Storage.Google.Drive;
using FlowSynx.Core.Services;

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
            .AddFlowSynxStorageServices()
            .AddMultiKeyCache<string, string, StorageNormsInfo>()
            .AddScoped<IStorageNormsParser, StorageNormsParser>()
            .AddScoped<INamespaceParser, NamespaceParser>()
            .AddScoped<ISpecificationsParser, SpecificationsParser>()
            .AddScoped<IStorageFilter, StorageFilter>()
            .AddScoped<IStorageService, StorageService>();

        return services;
    }
    
    public static IServiceCollection AddFlowSynxConfiguration(this IServiceCollection services, string configurationPath)
    {
        services
            .AddSingleton(new ConfigurationPath { Path = configurationPath })
            .AddConfiguration();
        return services;
    }

    public static IServiceCollection AddFlowSynxStorageServices(this IServiceCollection services)
    {
        services
            .AddScoped<IEntityCopier, EntityCopier>()
            .AddScoped<IEntityMover, EntityMover>()
            .AddScoped<IEntityChecker, EntityChecker>()
            .AddScoped<IEntityCompress, EntityCompress>();
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
        services.AddScoped<IPlugin, AzureFileStorage>();
        services.AddScoped<IPlugin, AzureBlobStorage>();
        services.AddScoped<IPlugin, GoogleCloudStorage>();
        services.AddScoped<IPlugin, AmazonS3Storage>();
        services.AddScoped<IPlugin, GoogleDriveStorage>();
        return services;
    }
}