using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Services;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.LocalFileSystem;

namespace FlowSynx.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
            .AddScoped<ISystemClock, SystemClock>()
            .AddSingleton(typeof(ICacheService<string, Plugin>), typeof(CacheService<string, Plugin>))
            //.AddScoped<IWorkflowExecutor, WorkflowExecutor>()
            .AddScoped<IHashService, HashService>()
            .AddScoped<IPluginService, PluginService>()
            .AddScoped<IPluginTypeService, PluginTypeService>()
            .AddScoped<IPluginSpecificationsService, PluginSpecificationsService>();

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
        services.AddScoped<Plugin, LocalFileSystemPlugin>();
        return services;
    }
}