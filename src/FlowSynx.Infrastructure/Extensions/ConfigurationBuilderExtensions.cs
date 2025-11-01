using FlowSynx.Application.Secrets;
using FlowSynx.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;

namespace FlowSynx.Infrastructure.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddSecrets(
        this IConfigurationBuilder builder, 
        ISecretProvider? provider)
    {
        if (provider == null) return builder;
        return builder.Add(new SecretConfigurationSource(provider));
    }
}