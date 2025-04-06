using FlowSynx.PluginCore;

namespace FlowSynx.Application.Services;

public interface IPluginTypeService
{
    Task<IPlugin> Get(string userId, object? type, CancellationToken cancellationToken);
}