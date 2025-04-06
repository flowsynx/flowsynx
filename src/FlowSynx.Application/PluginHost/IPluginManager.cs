namespace FlowSynx.Application.PluginHost;

public interface IPluginManager
{
    Task InstallAsync(string pluginName, string pluginVersion, CancellationToken cancellationToken);
    Task UpdateAsync(string pluginName, string oldVersion, string newPluginVersion, CancellationToken cancellationToken);
    Task Uninstall(string pluginName, string version, CancellationToken cancellationToken);
}