using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Services;
using FlowSynx.Infrastructure.Workflow;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Workflow;
using FlowSynx.Infrastructure.Serialization;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.Infrastructure.PluginHost.Manager;
using FlowSynx.Infrastructure.PluginHost.Cache;
using FlowSynx.Application.PluginHost.Manager;

namespace FlowSynx.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginManager(this IServiceCollection services)
    {
        services
            .AddSingleton<IPluginCacheService, PluginCacheService>()
            .AddScoped<IPluginDownloader, PluginDownloader>()
            .AddScoped<IPluginManager, PluginManager>()
            .AddSingleton<IPluginCacheKeyGeneratorService, PluginCacheKeyGeneratorService>()
            .AddScoped<IPluginTypeService, PluginTypeService>()
            .AddScoped<IPluginSpecificationsService, PluginSpecificationsService>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
            .AddSingleton<ISystemClock, SystemClock>()
            .AddScoped<IExpressionParserFactory, ExpressionParserFactory>()
            .AddScoped<IPlaceholderReplacer, PlaceholderReplacer>()
            .AddScoped<IErrorHandlingResolver, ErrorHandlingResolver>()
            .AddScoped<ISemaphoreFactory, SemaphoreFactory>()
            .AddScoped<IWorkflowExecutionTracker, WorkflowExecutionTracker>()
            .AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>()
            .AddScoped<IWorkflowTaskExecutor, WorkflowTaskExecutor>()
            .AddSingleton<IErrorHandlingStrategyFactory, ErrorHandlingStrategyFactory>()
            .AddScoped<IWorkflowExecutor, WorkflowExecutor>()
            .AddScoped<IWorkflowValidator, WorkflowValidator>()
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
}