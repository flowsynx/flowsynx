using FlowSynx.Application.Configuration.System.Storage;

namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public class ResultStorageFactory: IResultStorageFactory
{
    private readonly IDictionary<string, ResultStorageProviderConfiguration> _providerConfigs;
    private readonly IEnumerable<IResultStorageProvider> _providers;
    private readonly StorageConfiguration _config;

    public ResultStorageFactory(
        StorageConfiguration config,
        IEnumerable<IResultStorageProvider> providers)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));

        _providerConfigs = _config.ResultStorage.Providers
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IResultStorageProvider GetDefaultProvider()
    {
        var defaultProvider = _config.ResultStorage.DefaultProvider;

        if (!_providerConfigs.TryGetValue(defaultProvider, out var config))
        {
            throw new InvalidOperationException($"No result storage configuration found for '{defaultProvider}'.");
        }

        var provider = _providers.FirstOrDefault(p =>
            string.Equals(p.Name, config.Name, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            throw new InvalidOperationException($"No result storage provider implementation found for type '{config.Name}'.");
        }

        if (provider is IConfigurableResultStorage configurable)
        {
            configurable.Configure(config.Configuration, _config.MaxSizeLimit);
        }

        return provider;
    }
}