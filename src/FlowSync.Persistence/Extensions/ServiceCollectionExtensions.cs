using FlowSync.Core.Configuration;
using FlowSync.Core.Plugins;
using FlowSync.Persistence.Json.IO;
using FlowSync.Persistence.Json.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSync.Persistence.Json.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSyncPersistence(this IServiceCollection services, string configurationPath)
    {
        services.AddSingleton(new ConfigurationPath { Path = configurationPath});

        services
            .AddTransient<IFileReader, FileReader>()
            .AddTransient<IFileWriter, FileWriter>()
            .AddSingleton<IPluginsManager, PluginsManager>()
            .AddSingleton<IConfigurationManager, ConfigurationManager>();

        return services;
    }
}