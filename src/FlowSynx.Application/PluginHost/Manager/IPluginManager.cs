namespace FlowSynx.Application.PluginHost.Manager;

public interface IPluginManager
{
    Task InstallAsync(string pluginType, string pluginVersion, CancellationToken cancellationToken);
    Task UpdateAsync(string pluginType, string oldVersion, string newPluginVersion, CancellationToken cancellationToken);
    Task Uninstall(string pluginType, string version, CancellationToken cancellationToken);
}