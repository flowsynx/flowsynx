using FlowSynx.Application.Serializations;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Serializations.Json;
using FlowSynx.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSystemClock(
        this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        //services.AddScoped(typeof(ILogger<>), typeof(TenantAwareLogger<>));
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

    //public static IServiceCollection AddSecretService(
    //    this IServiceCollection services, 
    //    IConfiguration configuration)
    //{
    //    using var serviceProviderScope = services.BuildServiceProvider().CreateScope();
    //    var logger = serviceProviderScope.ServiceProvider.GetRequiredService<ILogger<SecretFactory>>();

    //    logger.LogInformation("Initializing secret provider");

    //    var secretConfiguration = new SecretConfiguration();
    //    configuration.GetSection("Core:Secrets").Bind(secretConfiguration);
    //    services.AddSingleton(secretConfiguration);

    //    secretConfiguration.ValidateSecretProviders(logger);

    //    services.AddSingleton<ISecretProvider, InfisicalSecretProvider>();
    //    services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
    //    services.AddSingleton<ISecretProvider, HashiCorpVaultSecretProvider>();
    //    services.AddSingleton<ISecretProvider, AwsSecretsManagerSecretProvider>();
    //    services.AddSingleton<ISecretFactory, SecretFactory>();

    //    return services;
    //}
}
