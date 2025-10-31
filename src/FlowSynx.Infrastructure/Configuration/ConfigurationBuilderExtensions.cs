using System;
using FlowSynx.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Extension methods for registering Infisical as a configuration source.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds Infisical as a configuration source to the provided configuration builder.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder.</param>
    /// <param name="options">Infisical configuration options.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostics.</param>
    /// <param name="clientFactory">Optional custom client factory (useful for testing).</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddInfisical(
        this IConfigurationBuilder configurationBuilder,
        InfisicalConfiguration options,
        ILoggerFactory? loggerFactory = null,
        Func<InfisicalConfiguration, ILoggerFactory?, IInfisicalSecretClient>? clientFactory = null)
    {
        if (configurationBuilder is null)
            throw new ArgumentNullException(nameof(configurationBuilder));

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        configurationBuilder.Add(new InfisicalConfigurationSource(options, clientFactory, loggerFactory));
        return configurationBuilder;
    }
}
