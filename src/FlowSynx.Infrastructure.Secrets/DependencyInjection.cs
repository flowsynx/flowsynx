using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Infrastructure.Secrets.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Secrets;

public static class DependencyInjection
{
    public static IServiceCollection AddSecrets(this IServiceCollection services)
    {
        services.AddScoped<ISecretProviderFactory, SecretProviderFactory>();
        return services;
    }
}