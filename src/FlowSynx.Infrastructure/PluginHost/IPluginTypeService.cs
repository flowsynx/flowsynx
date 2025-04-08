using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginTypeService
{
    Task<IPlugin> Get(string userId, object? type, CancellationToken cancellationToken);
}