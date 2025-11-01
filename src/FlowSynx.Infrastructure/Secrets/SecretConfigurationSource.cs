using FlowSynx.Application.Secrets;
using Microsoft.Extensions.Configuration;

namespace FlowSynx.Infrastructure.Secrets;

public class SecretConfigurationSource : IConfigurationSource
{
    private readonly ISecretProvider _secretProvider;

    public SecretConfigurationSource(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretConfigurationProvider(_secretProvider);
    }
}