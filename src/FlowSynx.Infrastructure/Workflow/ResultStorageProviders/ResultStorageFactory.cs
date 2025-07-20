using FlowSynx.Application.Configuration;

namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public class ResultStorageFactory: IResultStorageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDictionary<string, ResultStorageProviderConfiguration> _providerConfigs;
    private readonly IEnumerable<IResultStorageProvider> _providers;
    private readonly StorageConfiguration _config;

    public ResultStorageFactory(
        IServiceProvider serviceProvider,
        StorageConfiguration config,
        IEnumerable<IResultStorageProvider> providers)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _providers = providers ?? throw new ArgumentNullException(nameof(serviceProvider));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        _providerConfigs = config.ResultStorage.Providers
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