using FlowSynx.Domain.Entities.PluignConfig;

namespace FlowSynx.Domain.Interfaces;

public interface IPluginConfigurationService
{
    Task<IReadOnlyCollection<PluginConfiguration>> All(string userId, CancellationToken cancellationToken);
    Task<PluginConfiguration?> Get(string userId, string configId, CancellationToken cancellationToken);
    Task<bool> IsExist(string userId, string configId, CancellationToken cancellationToken);
    Task Add(PluginConfiguration configuration, CancellationToken cancellationToken);
    Task<bool> Delete(PluginConfiguration configuration, CancellationToken cancellationToken);
}