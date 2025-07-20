using FlowSynx.Application.Configuration;

namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public class ResultStorageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDictionary<string, ResultStorageProviderConfiguration> _providerConfigs;
    private readonly IEnumerable<IResultStorageProvider> _providers;
    private readonly long _maxLimitSize;

    public ResultStorageFactory(
        IServiceProvider serviceProvider,
        StorageConfiguration config,
        IEnumerable<IResultStorageProvider> providers)
    {
        _serviceProvider = serviceProvider;
        _providers = providers;

        _maxLimitSize = config.MaxSizeLimit;
        _providerConfigs = config.ResultStorage.Providers
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IResultStorageProvider GetProvider(string name)
    {
        if (!_providerConfigs.TryGetValue(name, out var config))
        {
            throw new InvalidOperationException($"No result storage configuration found for '{name}'.");
        }

        var provider = _providers.FirstOrDefault(p =>
            string.Equals(p.Type, config.Type, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            throw new InvalidOperationException($"No result storage provider implementation found for type '{config.Type}'.");
        }

        if (provider is IConfigurableResultStorage configurable)
        {
            configurable.Configure(config.Configuration, _maxLimitSize);
        }

        return provider;
    }
}