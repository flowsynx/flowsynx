using FlowSynx.Domain.Entities.Plugin;
namespace FlowSynx.Domain.Interfaces;

public interface IPluginService
{
    Task<IReadOnlyCollection<PluginEntity>> All(string userId, CancellationToken cancellationToken);
    Task<PluginEntity?> Get(string userId, Guid pluginId, CancellationToken cancellationToken);
    Task<PluginEntity?> Get(string userId, string pluginType, string pluginVersion, CancellationToken cancellationToken);
    Task<bool> IsExist(string userId, string pluginType, string pluginVersion, CancellationToken cancellationToken);
    Task Add(PluginEntity pluginEntity, CancellationToken cancellationToken);
    Task<bool> Delete(PluginEntity pluginEntity, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}