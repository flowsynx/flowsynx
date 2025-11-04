using FlowSynx.Application.Secrets;
using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Secrets.Infisical;

public class InfisicalSecretProvider : ISecretProvider, IConfigurableSecret
{
    private readonly ILogger<InfisicalSecretProvider>? _logger;
    private readonly InfisicalConfiguration _options = new InfisicalConfiguration();

    public InfisicalSecretProvider(
        ILogger<InfisicalSecretProvider>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "Infisical";

    public void Configure(Dictionary<string, string> configuration)
    {
        _options.HostUri = configuration.GetValueOrDefault("HostUri", string.Empty);
        _options.EnvironmentSlug = configuration.GetValueOrDefault("EnvironmentSlug", string.Empty);
        _options.ProjectId = configuration.GetValueOrDefault("ProjectId", string.Empty);
        _options.SecretPath = configuration.GetValueOrDefault("SecretPath", string.Empty);
        _options.ClientId = configuration.GetValueOrDefault("ClientId", string.Empty);
        _options.ClientSecret = configuration.GetValueOrDefault("ClientSecret", string.Empty);
    }

    public async Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredOptions();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await ExecuteInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            var environment = string.IsNullOrWhiteSpace(_options.EnvironmentSlug)
                ? "<unspecified>"
                : _options.EnvironmentSlug;

            var message = $"Error retrieving configuration secrets from Infisical for environment '{environment}'.";
            _logger?.LogWarning(ex, message);

            throw new Exception(message, ex);
        }
    }

    // Executes the Infisical SDK calls to retrieve and normalize secrets.
    protected virtual async Task<IReadOnlyCollection<KeyValuePair<string, string>>> ExecuteInternalAsync(
        CancellationToken cancellationToken)
    {
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

        await infisicalClient
            .Auth()
            .UniversalAuth()
            .LoginAsync(_options.ClientId, _options.ClientSecret)
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

    private void ValidateRequiredOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ProjectId))
            throw new InvalidOperationException("Infisical ProjectId is required when using Infisical as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.EnvironmentSlug))
            throw new InvalidOperationException("Infisical EnvironmentSlug is required when using Infisical as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("Infisical ClientId is required when using Infisical as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("Infisical ClientSecret is required when using Infisical as a configuration source.");
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
