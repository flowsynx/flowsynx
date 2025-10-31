using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Configuration provider that maps Infisical secrets to the ASP.NET configuration pipeline.
/// </summary>
public sealed class InfisicalConfigurationProvider : ConfigurationProvider
{
    private readonly IInfisicalSecretClient _secretClient;
    private readonly ILogger<InfisicalConfigurationProvider>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfisicalConfigurationProvider"/> class.
    /// </summary>
    /// <param name="secretClient">Client responsible for retrieving Infisical secrets.</param>
    /// <param name="logger">Optional logger used for diagnostics.</param>
    public InfisicalConfigurationProvider(
        IInfisicalSecretClient secretClient,
        ILogger<InfisicalConfigurationProvider>? logger = null)
    {
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Load()
    {
        try
        {
            var secrets = _secretClient
                .GetSecretsAsync()
                .GetAwaiter()
                .GetResult();

            Data = secrets.Count == 0
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(secrets.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var secret in secrets)
            {
                Data[secret.Key] = secret.Value;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load configuration from Infisical.");
            throw new InfisicalConfigurationException("Failed to load configuration from Infisical.", ex);
        }
    }
}
