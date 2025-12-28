using FlowSynx.Application.Configuration.Core.Secrets;
using FlowSynx.Application.Configuration.System.Localization;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Secrets;
using FlowSynx.Application.Serializations;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Localizations;
using FlowSynx.Infrastructure.Secrets;
using FlowSynx.Infrastructure.Secrets.AwsSecretsManager;
using FlowSynx.Infrastructure.Secrets.AzureKeyVault;
using FlowSynx.Infrastructure.Secrets.HashiCorpVault;
using FlowSynx.Infrastructure.Secrets.Infisical;
using FlowSynx.Infrastructure.Serializations.Json;
using FlowSynx.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSystemClock(
        this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        return services;
    }

    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services
            .AddSingleton<INormalizer, JsonNormalizer>()
            .AddSingleton<IObjectParser, JsonObjectParser>()
            .AddSingleton<ISerializer, JsonSerializer>()
            .AddSingleton<IDeserializer, JsonDeserializer>();

        return services;
    }

    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<JsonLocalization>>();

        var localizationConfiguration = new LocalizationConfiguration();
        configuration.GetSection("System:Localization").Bind(localizationConfiguration);
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

    public static IServiceCollection AddSecretService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
        var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<SecretFactory>>();

        logger.LogInformation("Initializing secret provider");

        var secretConfiguration = new SecretConfiguration();
        configuration.GetSection("Core:Secrets").Bind(secretConfiguration);
        services.AddSingleton(secretConfiguration);

        secretConfiguration.ValidateSecretProviders(logger);

        services.AddSingleton<ISecretProvider, InfisicalSecretProvider>();
        services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
        services.AddSingleton<ISecretProvider, HashiCorpVaultSecretProvider>();
        services.AddSingleton<ISecretProvider, AwsSecretsManagerSecretProvider>();
        services.AddSingleton<ISecretFactory, SecretFactory>();

        return services;
    }
}
