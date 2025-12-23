namespace FlowSynx.Application.PluginHost.Manager;

public interface IPluginManager
{
    Task InstallAsync(
        string pluginType, 
        string? currentVersion, 
        CancellationToken cancellationToken);

    Task UpdateAsync(
        string pluginType, 
        string currentVersion, 
        string? targetVersion, 
        CancellationToken cancellationToken);

    Task Uninstall(
        string pluginType, 
        string currentVersion, 
        CancellationToken cancellationToken);

    Task<(IReadOnlyCollection<PluginFullDetailsInfo> Items, int TotalCount)> GetPluginsFullDetailsListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}