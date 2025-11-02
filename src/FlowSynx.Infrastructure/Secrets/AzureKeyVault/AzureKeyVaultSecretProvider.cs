using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FlowSynx.Application.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Secrets.AzureKeyVault;

public class AzureKeyVaultSecretProvider : ISecretProvider, IConfigurableSecret
{
    private readonly ILogger<AzureKeyVaultSecretProvider>? _logger;
    private AzureKeyVaultConfiguration _options = new();

    public AzureKeyVaultSecretProvider(ILogger<AzureKeyVaultSecretProvider>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "AzureKeyVault";

    public void Configure(Dictionary<string, string> configuration)
    {
        _options.VaultUri = configuration.GetValueOrDefault("VaultUri", string.Empty);
        _options.TenantId = configuration.GetValueOrDefault("TenantId", string.Empty);
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
            var client = CreateClient();

            var secrets = new List<KeyValuePair<string, string>>();
            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip disabled or deleted secrets
                if (secretProperties.Enabled != true)
                    continue;

                try
                {
                    var secret = await client.GetSecretAsync(secretProperties.Name, cancellationToken: cancellationToken);
                    if (secret.Value?.Value is { } value && !string.IsNullOrWhiteSpace(value))
                    {
                        var normalizedKey = NormalizeKey(secret.Value.Name);
                        secrets.Add(new KeyValuePair<string, string>(normalizedKey, value));
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to retrieve secret '{SecretName}' from Azure Key Vault.", secretProperties.Name);
                }
            }

            return secrets;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Unexpected error while retrieving configuration secrets from Azure Key Vault.");
            throw;
        }
    }

    private void ValidateRequiredOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.VaultUri))
            throw new InvalidOperationException("Azure Key Vault URI is required when using Azure Key Vault as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("Azure ClientId is required when using Azure Key Vault as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("Azure ClientSecret is required when using Azure Key Vault as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.TenantId))
            throw new InvalidOperationException("Azure TenantId is required when using Azure Key Vault as a configuration source.");
    }

    private SecretClient CreateClient()
    {
        if (!Uri.TryCreate(_options.VaultUri, UriKind.Absolute, out var vaultUri))
            throw new InvalidOperationException($"Invalid Azure Key Vault URI '{_options.VaultUri}'.");

        var credential = new ClientSecretCredential(
            _options.TenantId,
            _options.ClientId,
            _options.ClientSecret
        );

        return new SecretClient(vaultUri, credential);
    }

    private static string NormalizeKey(string secretKey)
    {
        var trimmedKey = secretKey.Trim();

        if (trimmedKey.Length == 0)
            return string.Empty;

        // Normalize to configuration format, similar to Infisical
        var normalized = trimmedKey.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}