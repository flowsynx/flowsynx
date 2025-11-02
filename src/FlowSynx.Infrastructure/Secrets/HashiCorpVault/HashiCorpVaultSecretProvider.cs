using FlowSynx.Application.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;

namespace FlowSynx.Infrastructure.Secrets.HashiCorpVault;

public class HashiCorpVaultSecretProvider : ISecretProvider, IConfigurableSecret
{
    private readonly ILogger<HashiCorpVaultSecretProvider>? _logger;
    private readonly HashiCorpVaultConfiguration _options = new();

    public HashiCorpVaultSecretProvider(ILogger<HashiCorpVaultSecretProvider>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "HashiCorpVault";

    public void Configure(Dictionary<string, string> configuration)
    {
        _options.VaultUri = configuration.GetValueOrDefault("VaultUri", string.Empty);
        _options.Token = configuration.GetValueOrDefault("Token", string.Empty);
        _options.MountPoint = configuration.GetValueOrDefault("MountPoint", "secret");
        _options.SecretPath = configuration.GetValueOrDefault("SecretPath", string.Empty);
        _options.RoleId = configuration.GetValueOrDefault("RoleId", string.Empty);
        _options.SecretId = configuration.GetValueOrDefault("SecretId", string.Empty);
        _options.AuthMethod = configuration.GetValueOrDefault("AuthMethod", "token"); // "token" or "approle"
    }

    public async Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredOptions();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var authMethod = CreateAuthMethod();
            var vaultClientSettings = new VaultClientSettings(_options.VaultUri, authMethod);
            var vaultClient = new VaultClient(vaultClientSettings);

            // HashiCorp Vault has a variety of secret engines; we assume KV v2
            var kv2 = vaultClient.V1.Secrets.KeyValue.V2;
            var secretPath = _options.SecretPath.Trim('/');

            var secretData = await kv2.ReadSecretAsync(
                path: secretPath,
                mountPoint: _options.MountPoint
            );

            if (secretData?.Data?.Data is null || secretData.Data.Data.Count == 0)
                return Array.Empty<KeyValuePair<string, string>>();

            var result = new List<KeyValuePair<string, string>>();
            foreach (var kvp in secretData.Data.Data)
            {
                var key = NormalizeKey(kvp.Key);
                var value = kvp.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                {
                    result.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            var message = "Unexpected error while retrieving configuration secrets from HashiCorp Vault.";
            _logger?.LogError(ex, message);
            throw new InvalidOperationException(message, ex);
        }
    }

    private void ValidateRequiredOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.VaultUri))
            throw new InvalidOperationException("HashiCorp Vault URI is required when using HashiCorp Vault as a configuration source.");

        if (_options.AuthMethod.Equals("token", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_options.Token))
                throw new InvalidOperationException("Vault Token is required for token-based authentication.");
        }
        else if (_options.AuthMethod.Equals("approle", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_options.RoleId))
                throw new InvalidOperationException("Vault RoleId is required for AppRole authentication.");
            if (string.IsNullOrWhiteSpace(_options.SecretId))
                throw new InvalidOperationException("Vault SecretId is required for AppRole authentication.");
        }
        else
        {
            throw new InvalidOperationException($"Unsupported Vault authentication method '{_options.AuthMethod}'. Supported: 'token', 'approle'.");
        }

        if (string.IsNullOrWhiteSpace(_options.SecretPath))
            throw new InvalidOperationException("Vault SecretPath is required when using HashiCorp Vault as a configuration source.");
    }

    private IAuthMethodInfo CreateAuthMethod()
    {
        if (_options.AuthMethod.Equals("token", StringComparison.OrdinalIgnoreCase))
        {
            return new TokenAuthMethodInfo(_options.Token);
        }

        if (_options.AuthMethod.Equals("approle", StringComparison.OrdinalIgnoreCase))
        {
            return new AppRoleAuthMethodInfo(_options.RoleId, _options.SecretId);
        }

        throw new InvalidOperationException($"Unsupported Vault authentication method '{_options.AuthMethod}'.");
    }

    private static string NormalizeKey(string secretKey)
    {
        var trimmedKey = secretKey.Trim();
        if (trimmedKey.Length == 0)
            return string.Empty;

        var normalized = trimmedKey.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}