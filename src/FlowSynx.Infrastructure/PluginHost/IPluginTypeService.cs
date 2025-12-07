using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginTypeService
{
    Task<IPlugin> Get(
        string userId, 
        string? type,
        Dictionary<string, object?>? specification,
        CancellationToken cancellationToken);
}