﻿using System.IO;
using System.Reflection;
using FlowSync.Abstractions;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common.Services;
using FlowSync.Core.Configuration;
using FlowSync.Core.Parers.Date;
using FlowSync.Core.Parers.Namespace;
using FlowSync.Core.Parers.Norms.Storage;
using FlowSync.Core.Parers.Size;
using FlowSync.Core.Parers.Sort;
using FlowSync.Core.Plugins;
using FlowSync.Core.Serialization;
using FlowSync.Infrastructure.IO;
using FlowSync.Infrastructure.Parers.Date;
using FlowSync.Infrastructure.Parers.Namespace;
using FlowSync.Infrastructure.Parers.Norms.Storage;
using FlowSync.Infrastructure.Parers.Size;
using FlowSync.Infrastructure.Parers.Sort;
using FlowSync.Infrastructure.Serialization.Json;
using FlowSync.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSync.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSyncInfrastructure(this IServiceCollection services)
    {
        services.AddTransient<ISerializer, NewtonsoftSerializer>();
        services.AddTransient<IDeserializer, NewtonsoftDeserializer>();
        services.AddTransient<ISystemClock, SystemClock>();
        services.AddTransient<IOperatingSystemInfo, OperatingSystemInfo>();
        services.AddScoped<IDateParser, DateParser>();
        services.AddScoped<ISizeParser, SizeParser>();
        services.AddScoped<ISortParser, SortParser>();
        services.AddScoped<IStorageNormsParser, StorageNormsParser>();
        services.AddScoped<INamespaceParser, NamespaceParser>();
        return services;
    }

    public static IServiceCollection AddFlowSyncConfigurationManager(this IServiceCollection services, string configurationPath)
    {
        services.AddSingleton(new ConfigurationPath { Path = configurationPath });

        services.AddTransient<IFileReader, FileReader>();
        services.AddTransient<IFileWriter, FileWriter>();
        services.AddScoped<IConfigurationManager, ConfigurationManager>();
        return services;
    }

    public static IServiceCollection AddFlowSyncPluginsManager(this IServiceCollection services)
    {
        services.RegisterFileSystemPlugins();
        services.AddScoped<IPluginsManager, PluginsManager>();
        return services;
    }

    public static IServiceCollection RegisterFileSystemPlugins(this IServiceCollection services)
    {
        var pluginType = typeof(IPlugin);
        var path = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(path);
        var dllFiles = directory.EnumerateFiles("*.dll", SearchOption.AllDirectories);

        var assemblies = dllFiles.Select(file => Assembly.LoadFile(file.FullName)).ToList();
        var types = assemblies
            .SelectMany(s => s.GetTypes())
            .Where(p => p.GetInterfaces().Contains(pluginType) && p is {IsClass: true, IsPublic: true}).Select(s => new
            {
                Service = pluginType,
                Implementation = s
            }).ToList();

        types.ForEach(x => services.AddScoped(x.Service, x.Implementation));
        return services;
    }
}