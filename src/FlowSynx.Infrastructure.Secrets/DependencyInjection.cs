using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddFlowSynxSecretManagement(this IServiceCollection services)
    {
        services.AddSingleton<ISecretProviderFactory, SecretProviderFactory>();
        return services;
    }
}