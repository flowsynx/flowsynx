using FlowSynx.Application.Secrets;
using Microsoft.Extensions.Configuration;

namespace FlowSynx.Infrastructure.Secrets;

public class SecretConfigurationProvider : ConfigurationProvider
{
    private readonly ISecretProvider _secretProvider;

    public SecretConfigurationProvider(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public override void Load()
    {
        var secrets = _secretProvider
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
}