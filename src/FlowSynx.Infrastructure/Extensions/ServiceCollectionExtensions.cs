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
using FlowSynx.Application.Localizations;
using Microsoft.Extensions.Logging;
using FlowSynx.Infrastructure.Localizations;
using Microsoft.Extensions.Configuration;
using FlowSynx.Application.Configuration;
using FlowSynx.Infrastructure.Workflow.ResultStorageProviders;
using FlowSynx.Infrastructure.Workflow.ManualApprovals;

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
            .AddSingleton<IWorkflowCancellationRegistry, WorkflowCancellationRegistry>()
            .AddScoped<IExpressionParserFactory, ExpressionParserFactory>()
            .AddScoped<IPlaceholderReplacer, PlaceholderReplacer>()
            .AddScoped<IErrorHandlingResolver, ErrorHandlingResolver>()
            .AddScoped<ISemaphoreFactory, SemaphoreFactory>()
            .AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>()
            .AddScoped<IWorkflowTaskExecutor, WorkflowTaskExecutor>()
            .AddSingleton<IErrorHandlingStrategyFactory, ErrorHandlingStrategyFactory>()
            .AddScoped<IWorkflowValidator, WorkflowValidator>()
            .AddScoped<IManualApprovalService, ManualApprovalService>()
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

    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<JsonLocalization>>();

        var localizationConfiguration = new LocalizationConfiguration();
        configuration.GetSection("Localization").Bind(localizationConfiguration);
        services.AddSingleton(localizationConfiguration);

        var language = Language.GetByCode(localizationConfiguration.Language);
        if (language == null)
        {
            logger.LogWarning("The specified language '{Language}' is invalid or unsupported. " +
                "The FlowSynx language will default to English.", localizationConfiguration.Language);
            language = Language.English;
        }
        logger.LogInformation("FlowSynx language has been set to '{Language}'", language.Name);

        services.AddSingleton<ILocalization>(provider =>
        {
            var jsonLocalization = new JsonLocalization(language, logger);
            Localization.Instance = jsonLocalization;
            return jsonLocalization;
        });

        return services;
    }

    public static IServiceCollection AddEncryptionService(this IServiceCollection services, IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();

        var encryptionConfiguration = new EncryptionConfiguration();
        configuration.GetSection("Encryption").Bind(encryptionConfiguration);
        services.AddSingleton(encryptionConfiguration);

        services.AddSingleton<IEncryptionService>(provider =>
        {
            var jsonLocalization = new EncryptionService(encryptionConfiguration.Key);
            return jsonLocalization;
        });

        return services;
    }

    public static IServiceCollection AddResultStorageService(this IServiceCollection services, IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<StorageConfiguration>>();

        logger.LogInformation("Initializing storage provider");

        var storageConfiguration = new StorageConfiguration();
        configuration.GetSection("Storage").Bind(storageConfiguration);
        services.AddSingleton(storageConfiguration);

        storageConfiguration.ResultStorage.ValidateResultStorage(logger);

        services.AddSingleton<IResultStorageProvider, LocalResultStorageProvider>();
        services.AddSingleton<IResultStorageFactory, ResultStorageFactory>();

        return services;
    }

    public static IServiceCollection AddInMemoryWorkflowQueueService(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowExecutionQueue, InMemoryWorkflowExecutionQueue>();
        return services;
    }
}