using FlowSynx.Application.Configuration;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Secrets;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Infrastructure.Localizations;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Infrastructure.PluginHost.Cache;
using FlowSynx.Infrastructure.PluginHost.Manager;
using FlowSynx.Infrastructure.Secrets;
using FlowSynx.Infrastructure.Secrets.AzureKeyVault;
using FlowSynx.Infrastructure.Secrets.Infisical;
using FlowSynx.Infrastructure.Serialization;
using FlowSynx.Infrastructure.Services;
using FlowSynx.Infrastructure.Workflow;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.Infrastructure.Workflow.ManualApprovals;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.Infrastructure.Workflow.ResultStorageProviders;
using FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;
using FlowSynx.Infrastructure.Workflow.Triggers.TimeBased;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginManager(
        this IServiceCollection services)
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

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services
            .AddMemoryCache()
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
            .AddSingleton<IWorkflowSchemaValidator, WorkflowSchemaValidator>()
            .AddScoped<IManualApprovalService, ManualApprovalService>()
            .AddSingleton<IWorkflowHttpListener, InMemoryWorkflowHttpListener>()
            .AddScoped<IWorkflowTriggerProcessor, WorkflowTimeTriggerProcessor>()
            .AddScoped<IWorkflowTriggerProcessor, WorkflowHttpTriggerProcessor>();

        return services;
    }

    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services
            .AddSingleton<IJsonSerializer, JsonSerializer>()
            .AddSingleton<IJsonDeserializer, JsonDeserializer>();

        return services;
    }

    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services, 
        IConfiguration configuration)
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

    public static IServiceCollection AddEncryptionService(
        this IServiceCollection services, 
        IConfiguration configuration)
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

    public static IServiceCollection AddResultStorageService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<ResultStorageFactory>>();

        logger.LogInformation("Initializing storage provider");

        var storageConfiguration = new StorageConfiguration();
        configuration.GetSection("Storage").Bind(storageConfiguration);

        if (!storageConfiguration.ResultStorage.Providers.Any())
        {
            storageConfiguration.ResultStorage.Providers.Add(new ResultStorageProviderConfiguration
            {
                Name = "Local",
                Configuration = new Dictionary<string, string>()
            });
        }

        services.AddSingleton(storageConfiguration);

        storageConfiguration.ResultStorage.ValidateResultStorage(logger);

        services.AddSingleton<IResultStorageProvider, LocalResultStorageProvider>();
        services.AddSingleton<IResultStorageFactory, ResultStorageFactory>();

        return services;
    }

    public static IServiceCollection AddInMemoryWorkflowQueueService(
        this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowExecutionQueue, InMemoryWorkflowExecutionQueue>();
        return services;
    }

    public static IServiceCollection AddSecretService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<SecretFactory>>();

        logger.LogInformation("Initializing secret provider");

        var secretConfiguration = new SecretConfiguration();
        configuration.GetSection("Secrets").Bind(secretConfiguration);
        services.AddSingleton(secretConfiguration);

        secretConfiguration.ValidateSecretProviders(logger);

        services.AddSingleton<ISecretProvider, InfisicalSecretProvider>();
        services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
        services.AddSingleton<ISecretFactory, SecretFactory>();

        return services;
    }
}
