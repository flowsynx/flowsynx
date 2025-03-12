using FlowSynx.Application.Model;

namespace FlowSynx.Application.Services;

public interface IPluginSpecificationsService
{
    Task<PluginSpecificationsResult> Validate(string type, Dictionary<string, object?>? specifications, 
        CancellationToken cancellationToken);
}