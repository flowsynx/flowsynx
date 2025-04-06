using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Services;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.LocalFileSystem;
using FlowSynx.Infrastructure.Workflow;
using FlowSynx.Plugins.Amazon.S3;
using FlowSynx.Plugins.Azure.Blobs;
using FlowSynx.Plugins.Azure.Files;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Application.PluginHost;
using Microsoft.Extensions.Configuration;

namespace FlowSynx.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginManager(this IServiceCollection services)
    {
        services
            .AddScoped<IExtractPluginSpecifications, ExtractPluginSpecifications>()
            .AddScoped<IPluginChecksumValidator, Sha256PluginChecksumValidator>()
            .AddScoped<IPluginDownloader, PluginDownloader>()
            .AddScoped<IPluginExtractor, PluginExtractor>()
            .AddScoped<IPluginLoader, PluginLoader>()
            .AddScoped<IPluginManager, PluginManager>()
            .AddScoped<IPluginTypeService, PluginTypeService>()
            .AddScoped<IPluginSpecificationsService, PluginSpecificationsService>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton(typeof(ICacheService<string, IPlugin>), typeof(CacheService<string, IPlugin>))
            .AddScoped<IWorkflowExecutor, WorkflowExecutor>()
            .AddScoped<IHashService, HashService>()
            .AddScoped<IWorkflowValidator, WorkflowValidator>()
            .AddScoped<IRetryService, RetryService>()
            .AddScoped<WorkflowTimeBasedTriggerProcessor>();

        return services;
    }

    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services
            .AddSingleton<IJsonSerializer, JsonSerializer>()
            .AddSingleton<IJsonDeserializer, JsonDeserializer>();

        return services;
    }

    public static IServiceCollection AddFlowSynxPlugins(this IServiceCollection services)
    {
        services
            .RegisterPlugins();
        return services;
    }

    public static IServiceCollection RegisterPlugins(this IServiceCollection services)
    {
        services.AddScoped<IPlugin, LocalFileSystemPlugin>();
        //services.AddScoped<Plugin, AmazonS3Plugin>();
        //services.AddScoped<Plugin, AzureBlobPlugin>();
        //services.AddScoped<Plugin, AzureFilePlugin>();
        return services;
    }
}