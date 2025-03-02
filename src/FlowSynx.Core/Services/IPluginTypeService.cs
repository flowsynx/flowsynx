using FlowSynx.PluginCore;

namespace FlowSynx.Core.Services;

public interface IPluginTypeService
{
    Task<Plugin> Get(string userId, object? type, CancellationToken cancellationToken);
}