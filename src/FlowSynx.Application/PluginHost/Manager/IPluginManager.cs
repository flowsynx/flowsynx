namespace FlowSynx.Application.PluginHost.Manager;

public interface IPluginManager
{
    Task InstallAsync(string pluginType, string? currentVersion, CancellationToken cancellationToken);
    Task UpdateAsync(string pluginType, string currentVersion, string? targetVersion, CancellationToken cancellationToken);
    Task Uninstall(string pluginType, string currentVersion, CancellationToken cancellationToken);
}