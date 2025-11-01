using FlowSynx.Application.Configuration;
using FlowSynx.Application.Secrets;

namespace FlowSynx.Infrastructure.Secrets;

public class SecretFactory : ISecretFactory
{
    private readonly IDictionary<string, SecretProviderConfiguration> _providerConfigs;
    private readonly IEnumerable<ISecretProvider> _providers;
    private readonly SecretConfiguration _config;

    public SecretFactory(
        SecretConfiguration config,
        IEnumerable<ISecretProvider> providers)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _providerConfigs = _config.Providers;
    }

    public ISecretProvider? GetDefaultProvider()
    {
        if (!_config.Enabled)
            return null;

        var defaultProvider = _config.DefaultProvider;

        if (!_providerConfigs.TryGetValue(defaultProvider, out var providerType))
        {
            throw new InvalidOperationException($"No secret configuration found for '{defaultProvider}'.");
        }

        var provider = _providers.FirstOrDefault(p => 
            string.Equals(p.Name, defaultProvider, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            throw new InvalidOperationException($"No secret provider implementation found for type '{providerType}'.");
        }

        if (provider is IConfigurableSecret configurable)
        {
            configurable.Configure(providerType);
        }

        return provider;
    }
}