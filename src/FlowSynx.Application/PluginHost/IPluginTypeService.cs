using FlowSynx.PluginCore;

namespace FlowSynx.Application.PluginHost;

public interface IPluginTypeService
{
    Task<IPlugin> Get(string userId, object? type, CancellationToken cancellationToken);
}