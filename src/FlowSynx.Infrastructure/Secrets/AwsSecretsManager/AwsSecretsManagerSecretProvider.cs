using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FlowSynx.Application.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Secrets.AwsSecretsManager;

public class AwsSecretsManagerSecretProvider : ISecretProvider, IConfigurableSecret
{
    private readonly ILogger<AwsSecretsManagerSecretProvider>? _logger;
    private readonly AwsSecretsManagerConfiguration _options = new();

    public AwsSecretsManagerSecretProvider(ILogger<AwsSecretsManagerSecretProvider>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "AwsSecretsManager";

    public void Configure(Dictionary<string, string> configuration)
    {
        _options.Region = configuration.GetValueOrDefault("Region", string.Empty);
        _options.AccessKey = configuration.GetValueOrDefault("AccessKey", string.Empty);
        _options.SecretKey = configuration.GetValueOrDefault("SecretKey", string.Empty);
        _options.SecretPrefix = configuration.GetValueOrDefault("SecretPrefix", string.Empty);
    }

    public async Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredOptions();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var client = CreateSecretsManagerClient();
            var result = new List<KeyValuePair<string, string>>();
            string? nextToken = null;

            do
            {
                var listResponse = await ListSecretsAsync(client, nextToken, cancellationToken).ConfigureAwait(false);
                await ProcessSecretsAsync(client, listResponse.SecretList, result, cancellationToken).ConfigureAwait(false);
                nextToken = listResponse.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            return result;
        }
        catch (Exception ex)
        {
            const string message = "Unexpected error while retrieving configuration secrets from AWS Secrets Manager.";
            _logger?.LogError(ex, message);
            throw new InvalidOperationException(message, ex);
        }
    }

    /// <summary>
    /// Creates an AWS Secrets Manager client that honors the configured region and credentials.
    /// </summary>
    private AmazonSecretsManagerClient CreateSecretsManagerClient()
    {
        var region = RegionEndpoint.GetBySystemName(_options.Region);
        return new AmazonSecretsManagerClient(_options.AccessKey, _options.SecretKey, region);
    }

    /// <summary>
    /// Lists secrets using pagination tokens to keep the iteration logic focused and testable.
    /// </summary>
    private static async Task<ListSecretsResponse> ListSecretsAsync(
        IAmazonSecretsManager client,
        string? nextToken,
        CancellationToken cancellationToken)
    {
        var listRequest = new ListSecretsRequest
        {
            MaxResults = 100,
            NextToken = nextToken
        };

        return await client.ListSecretsAsync(listRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes each secret metadata entry, filtering unwanted keys and retrieving allowed secrets.
    /// </summary>
    private async Task ProcessSecretsAsync(
        IAmazonSecretsManager client,
        List<SecretListEntry>? secrets,
        List<KeyValuePair<string, string>> result,
        CancellationToken cancellationToken)
    {
        if (secrets is null || secrets.Count == 0)
        {
            return;
        }

        foreach (var secret in secrets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsSecretAllowed(secret.Name))
            {
                continue;
            }

            await TryRetrieveSecretAsync(client, secret, result, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Determines if a secret should be processed based on the configured prefix.
    /// </summary>
    private bool IsSecretAllowed(string secretName) =>
        string.IsNullOrEmpty(_options.SecretPrefix) ||
        secretName.StartsWith(_options.SecretPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Attempts to download a secret value, logging known error cases and parsing successful responses.
    /// </summary>
    private async Task TryRetrieveSecretAsync(
        IAmazonSecretsManager client,
        SecretListEntry secret,
        List<KeyValuePair<string, string>> result,
        CancellationToken cancellationToken)
    {
        try
        {
            var getSecretResponse = await client.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = secret.ARN },
                cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(getSecretResponse.SecretString))
            {
                ParseSecretContent(result, secret.Name, getSecretResponse.SecretString);
            }
        }
        catch (ResourceNotFoundException ex)
        {
            _logger?.LogWarning(ex, "Secret '{SecretName}' not found in AWS Secrets Manager.", secret.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to retrieve secret '{SecretName}' from AWS Secrets Manager.", secret.Name);
        }
    }

    /// <summary>
    /// Ensures all required AWS settings are present before attempting to create a Secrets Manager client.
    /// </summary>
    private void ValidateRequiredOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Region))
        {
            throw new InvalidOperationException("AWS Region is required when using AWS Secrets Manager as a configuration source.");
        }

        if (string.IsNullOrWhiteSpace(_options.AccessKey))
        {
            throw new InvalidOperationException("AWS AccessKey is required when using AWS Secrets Manager as a configuration source.");
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("AWS SecretKey is required when using AWS Secrets Manager as a configuration source.");
        }
    }

    /// <summary>
    /// Breaks down the retrieved secret payload into normalized key/value pairs.
    /// </summary>
    private static void ParseSecretContent(
        List<KeyValuePair<string, string>> result,
        string secretName,
        string secretString)
    {
        try
        {
            // Attempt to parse JSON-based secret value
            using var jsonDoc = JsonDocument.Parse(secretString);
            var jsonRoot = jsonDoc.RootElement;
            if (jsonRoot.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonRoot.EnumerateObject())
                {
                    var key = NormalizeKey($"{secretName}:{property.Name}");
                    var value = property.Value.GetString() ?? property.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(key) && value is not null)
                    {
                        result.Add(new KeyValuePair<string, string>(key, value));
                    }
                }

                return;
            }
        }
        catch (JsonException)
        {
            // Not JSON - treat as single string secret
        }

        // Fallback: simple key/value
        var normalized = NormalizeKey(secretName);
        result.Add(new KeyValuePair<string, string>(normalized, secretString));
    }

    /// <summary>
    /// Normalizes AWS secret names into configuration keys the app configuration system can consume.
    /// </summary>
    private static string NormalizeKey(string secretKey)
    {
        var trimmedKey = secretKey.Trim();
        if (trimmedKey.Length == 0)
        {
            return string.Empty;
        }

        var normalized = trimmedKey.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}

