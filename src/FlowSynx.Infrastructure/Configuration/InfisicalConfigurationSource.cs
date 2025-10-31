using System;
using FlowSynx.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Provides the configuration source definition used to register the Infisical configuration provider.
/// </summary>
public sealed class InfisicalConfigurationSource : IConfigurationSource
{
    private readonly InfisicalConfiguration _options;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Func<InfisicalConfiguration, ILoggerFactory?, IInfisicalSecretClient> _clientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfisicalConfigurationSource"/> class.
    /// </summary>
    /// <param name="options">The Infisical configuration options.</param>
    /// <param name="clientFactory">Factory that constructs an Infisical secret client.</param>
    /// <param name="loggerFactory">Optional logger factory used to create provider loggers.</param>
    public InfisicalConfigurationSource(
        InfisicalConfiguration options,
        Func<InfisicalConfiguration, ILoggerFactory?, IInfisicalSecretClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory;
        _clientFactory = clientFactory ?? DefaultClientFactory;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var client = _clientFactory(_options, _loggerFactory);
        var providerLogger = _loggerFactory?.CreateLogger<InfisicalConfigurationProvider>();
        return new InfisicalConfigurationProvider(client, providerLogger);
    }

    private static IInfisicalSecretClient DefaultClientFactory(
        InfisicalConfiguration configuration,
        ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger<InfisicalSecretClient>();
        return new InfisicalSecretClient(configuration, logger);
    }
}
