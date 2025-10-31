using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowSynx.Application.Configuration;
using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Fetches configuration secrets from Infisical using the official SDK.
/// </summary>
public sealed class InfisicalSecretClient : IInfisicalSecretClient
{
    private readonly InfisicalConfiguration _options;
    private readonly ILogger<InfisicalSecretClient>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfisicalSecretClient"/> class.
    /// </summary>
    /// <param name="options">The Infisical configuration options.</param>
    /// <param name="logger">Optional logger used for diagnostic messages.</param>
    public InfisicalSecretClient(InfisicalConfiguration options, ILogger<InfisicalSecretClient>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(CancellationToken cancellationToken = default)
    {
        ValidateRequiredOptions();
        cancellationToken.ThrowIfCancellationRequested();

        var settingsBuilder = new InfisicalSdkSettingsBuilder();
        if (!string.IsNullOrWhiteSpace(_options.HostUri))
        {
            if (!Uri.TryCreate(_options.HostUri, UriKind.Absolute, out var hostUri))
            {
                throw new InvalidOperationException($"Invalid Infisical host URI '{_options.HostUri}'.");
            }

            settingsBuilder.WithHostUri(hostUri.ToString());
        }

        var infisicalClient = new InfisicalClient(settingsBuilder.Build());

        try
        {
            await infisicalClient
                .Auth()
                .UniversalAuth()
                .LoginAsync(_options.MachineIdentity.ClientId, _options.MachineIdentity.ClientSecret)
                .ConfigureAwait(false);

            var secrets = await infisicalClient
                .Secrets()
                .ListAsync(BuildListSecretOptions())
                .ConfigureAwait(false);

            if (secrets is null || secrets.Length == 0)
                return Array.Empty<KeyValuePair<string, string>>();

            var result = new List<KeyValuePair<string, string>>(secrets.Length);
            foreach (var secret in secrets)
            {
                if (string.IsNullOrWhiteSpace(secret.SecretKey))
                    continue;

                var normalizedKey = NormalizeKey(secret.SecretKey);
                if (string.IsNullOrEmpty(normalizedKey))
                    continue;

                if (secret.SecretValue is null)
                    continue;

                result.Add(new KeyValuePair<string, string>(normalizedKey, secret.SecretValue));
            }

            return result;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger?.LogWarning(ex, "Unexpected error while retrieving configuration secrets from Infisical.");
            throw;
        }
    }

    private void ValidateRequiredOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ProjectId))
            throw new InvalidOperationException("Infisical ProjectId is required when using Infisical as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.EnvironmentSlug))
            throw new InvalidOperationException("Infisical EnvironmentSlug is required when using Infisical as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.MachineIdentity.ClientId))
            throw new InvalidOperationException("Infisical MachineIdentity.ClientId is required when using Infisical as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.MachineIdentity.ClientSecret))
            throw new InvalidOperationException("Infisical MachineIdentity.ClientSecret is required when using Infisical as a configuration source.");
    }

    private ListSecretsOptions BuildListSecretOptions()
    {
        return new ListSecretsOptions
        {
            EnvironmentSlug = _options.EnvironmentSlug,
            ProjectId = _options.ProjectId,
            SecretPath = string.IsNullOrWhiteSpace(_options.SecretPath) ? "/" : _options.SecretPath,
            ExpandSecretReferences = true,
            SetSecretsAsEnvironmentVariables = false
        };
    }

    private static string NormalizeKey(string secretKey)
    {
        var trimmedKey = secretKey.Trim();

        if (trimmedKey.Length == 0)
            return string.Empty;

        // Support both colon and double underscore delimiters.
        var normalized = trimmedKey.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
