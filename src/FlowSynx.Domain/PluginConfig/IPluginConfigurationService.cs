namespace FlowSynx.Domain.PluginConfig;

public interface IPluginConfigurationService
{
    Task<IReadOnlyCollection<PluginConfigurationEntity>> All(string userId, CancellationToken cancellationToken);
    Task<PluginConfigurationEntity?> Get(string userId, Guid configId, CancellationToken cancellationToken);
    Task<PluginConfigurationEntity?> Get(string userId, string configName, CancellationToken cancellationToken);
    Task<bool> IsExist(string userId, Guid configId, CancellationToken cancellationToken);
    Task<bool> IsExist(string userId, string configName, CancellationToken cancellationToken);
    Task Add(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken);
    Task Update(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken);
    Task<bool> Delete(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}