using FlowSynx.Application.AI;
using FlowSynx.Application.Configuration.Core.AI;

namespace FlowSynx.Infrastructure.AI;

public class AiFactory : IAiFactory
{
    private readonly IDictionary<string, AiProviderConfiguration> _providerConfigs;
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly AiConfiguration _config;

    public AiFactory(
        AiConfiguration config,
        IEnumerable<IAiProvider> providers)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _providerConfigs = _config.Providers;
    }

    public IAiProvider GetDefaultProvider()
    {
        if (!_config.Enabled)
            throw new InvalidOperationException("AI configuration is not enabled.");

        var defaultProvider = _config.DefaultProvider;

        if (!_providerConfigs.TryGetValue(defaultProvider, out var providerType))
        {
            throw new InvalidOperationException($"No AI configuration found for '{defaultProvider}'.");
        }

        var provider = _providers.FirstOrDefault(p => 
            string.Equals(p.Name, defaultProvider, StringComparison.OrdinalIgnoreCase)) 
            ?? throw new InvalidOperationException($"No AI provider implementation found for type '{providerType}'.");

        if (provider is IConfigurableAi configurable)
        {
            configurable.Configure(providerType);
        }

        return provider;
    }
}