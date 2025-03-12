using FlowSynx.PluginCore;

namespace FlowSynx.Application.Services;

public interface IPluginService
{
    Task<IReadOnlyCollection<Plugin>> All(CancellationToken cancellationToken);
    Task<Plugin> Get(string type, CancellationToken cancellationToken);
    Task<bool> IsExist(string type, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}