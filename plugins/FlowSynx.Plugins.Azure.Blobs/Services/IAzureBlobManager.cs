using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Azure.Blobs.Services;

public interface IAzureBlobManager
{
    Task Create(PluginParameters parameters, CancellationToken cancellationToken);
    Task Delete(PluginParameters parameters, CancellationToken cancellationToken);
    Task<bool> Exist(PluginParameters parameters, CancellationToken cancellationToken);
    Task<IEnumerable<PluginContext>> List(PluginParameters parameters, CancellationToken cancellationToken);
    Task Purge(PluginParameters parameters, CancellationToken cancellationToken);
    Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken);
    Task Write(PluginParameters parameters, CancellationToken cancellationToken);
}