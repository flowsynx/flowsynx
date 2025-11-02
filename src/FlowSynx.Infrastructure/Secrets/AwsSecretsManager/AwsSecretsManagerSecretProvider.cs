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
    private AwsSecretsManagerConfiguration _options = new();

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
            var region = RegionEndpoint.GetBySystemName(_options.Region);
            var client = new AmazonSecretsManagerClient(_options.AccessKey, _options.SecretKey, region);

            var result = new List<KeyValuePair<string, string>>();
            string? nextToken = null;

            do
            {
                var listRequest = new ListSecretsRequest
                {
                    MaxResults = 100,
                    NextToken = nextToken
                };

                var listResponse = await client.ListSecretsAsync(listRequest, cancellationToken);

                foreach (var secret in listResponse.SecretList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(_options.SecretPrefix) &&
                        !secret.Name.StartsWith(_options.SecretPrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var getSecretResponse = await client.GetSecretValueAsync(
                            new GetSecretValueRequest { SecretId = secret.ARN },
                            cancellationToken);

                        if (getSecretResponse.SecretString is { Length: > 0 })
                        {
                            ParseSecretContent(result, secret.Name, getSecretResponse.SecretString);
                        }
                    }
                    catch (ResourceNotFoundException)
                    {
                        _logger?.LogWarning("Secret '{SecretName}' not found in AWS Secrets Manager.", secret.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to retrieve secret '{SecretName}' from AWS Secrets Manager.", secret.Name);
                    }
                }

                nextToken = listResponse.NextToken;

            } while (!string.IsNullOrEmpty(nextToken));

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Unexpected error while retrieving configuration secrets from AWS Secrets Manager.");
            throw;
        }
    }

    private void ValidateRequiredOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Region))
            throw new InvalidOperationException("AWS Region is required when using AWS Secrets Manager as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.AccessKey))
            throw new InvalidOperationException("AWS AccessKey is required when using AWS Secrets Manager as a configuration source.");

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException("AWS SecretKey is required when using AWS Secrets Manager as a configuration source.");
    }

    private static void ParseSecretContent(List<KeyValuePair<string, string>> result, string secretName, string secretString)
    {
        try
        {
            // Attempt to parse JSON-based secret value
            var jsonDoc = JsonDocument.Parse(secretString);
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
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
            // Not JSON — treat as single string secret
        }

        // Fallback: simple key/value
        var normalized = NormalizeKey(secretName);
        result.Add(new KeyValuePair<string, string>(normalized, secretString));
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