using FlowSynx.PluginCore;

namespace FlowSynx.Application.Services;

public interface IPluginTypeService
{
    Task<Plugin> Get(string userId, object? type, CancellationToken cancellationToken);
}