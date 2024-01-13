using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FlowSynx.Configuration;
using FlowSynx.Core.Behaviors;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;
using FlowSynx.Core.Storage.Filter;
using FlowSynx.Environment;
using FlowSynx.IO;
using FlowSynx.Parsers;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Storage;

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
            .AddScoped<IStorageNormsParser, StorageNormsParser>()
            .AddScoped<INamespaceParser, NamespaceParser>()
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

    public static IServiceCollection AddFlowSynxPlugins(this IServiceCollection services)
    {
        services
            .RegisterPlugins()
            .AddPluginManager();
        return services;
    }

    public static IServiceCollection RegisterPlugins(this IServiceCollection services)
    {
        var pluginType = typeof(IPlugin);

        var nameQueue = new Queue<AssemblyName>(AppDomain.CurrentDomain.GetAssemblies().Select(x=>x.GetName()));
        var alreadyProcessed = new HashSet<string>() { };
        var loadedAssemblies = new List<Assembly>();
        while (nameQueue.Any())
        {
            var name = nameQueue.Dequeue();
            var fullName = name.FullName;

            if (string.IsNullOrEmpty(fullName) || alreadyProcessed.Contains(fullName) || fullName.StartsWith("Microsoft.") || fullName.StartsWith("System."))
                continue;

            alreadyProcessed.Add(fullName);
            try
            {
                var newAssembly = Assembly.Load(name.FullName);
                loadedAssemblies.Add(newAssembly);
                foreach (var innerAsmName in newAssembly.GetReferencedAssemblies())
                    nameQueue.Enqueue(innerAsmName);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        var types = loadedAssemblies
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