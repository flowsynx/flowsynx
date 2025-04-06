using FlowSynx.PluginCore;
using FlowSynx.Plugins.Amazon.S3.Models;

namespace FlowSynx.Plugins.Amazon.S3.Services;

public interface IAmazonS3Manager
{
    Task Create(PluginParameters parameters, CancellationToken cancellationToken);
    Task Delete(PluginParameters parameters, CancellationToken cancellationToken);
    Task<bool> Exist(PluginParameters parameters, CancellationToken cancellationToken);
    Task<IEnumerable<PluginContext>> List(PluginParameters parameters, CancellationToken cancellationToken);
    Task Purge(PluginParameters parameters, CancellationToken cancellationToken);
    Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken);
    Task Write(PluginParameters parameters, CancellationToken cancellationToken);
}