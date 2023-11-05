using FlowSync.Core.Services;
using FlowSync.Persistence.Json.IO;
using FlowSync.Persistence.Json.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSync.Persistence.Json.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSyncJsonPersistence(this IServiceCollection services)
    {
        services
            .AddTransient<IFileReader, FileReader>()
            .AddTransient<IFileWriter, FileWriter>()
            .AddTransient<IPluginsManager, PluginsManager>()
            .AddTransient<IConfigurationManager, ConfigurationManager>();

        return services;
    }
}