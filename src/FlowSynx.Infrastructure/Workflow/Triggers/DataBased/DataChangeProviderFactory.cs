using System.Collections.Concurrent;
using FlowSynx.Application.Localizations;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Default implementation of <see cref="IDataChangeProviderFactory"/> that selects providers
/// using their declared provider keys.
/// </summary>
public class DataChangeProviderFactory : IDataChangeProviderFactory
{
    private readonly ConcurrentDictionary<string, IDataChangeProvider> _fallbackProviders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IDataChangeProvider> _providers;
    private readonly ILogger<DataChangeProviderFactory> _logger;
    private readonly ILocalization _localization;

    public DataChangeProviderFactory(
        IEnumerable<IDataChangeProvider> providers,
        ILogger<DataChangeProviderFactory> logger,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(localization);

        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
        _localization = localization;
    }

    public IDataChangeProvider Resolve(string providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new ArgumentException("Provider key cannot be null or whitespace.", nameof(providerKey));

        if (_providers.TryGetValue(providerKey, out var provider))
            return provider;

        return _fallbackProviders.GetOrAdd(providerKey, key =>
        {
            _logger.LogError(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_UnsupportedProvider", key));
            return new UnsupportedDataChangeProvider(key, _logger);
        });
    }

    private sealed class UnsupportedDataChangeProvider : IDataChangeProvider
    {
        private readonly ILogger _logger;

        public UnsupportedDataChangeProvider(string providerKey, ILogger logger)
        {
            ProviderKey = providerKey;
            _logger = logger;
        }

        public string ProviderKey { get; }

        public Task<IReadOnlyCollection<DataChangeEvent>> GetChangesAsync(
            DataTriggerConfiguration configuration,
            DataTriggerState state,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("No change provider configured for '{ProviderKey}'. Skipping polling.", ProviderKey);
            return Task.FromResult<IReadOnlyCollection<DataChangeEvent>>(Array.Empty<DataChangeEvent>());
        }
    }
}
