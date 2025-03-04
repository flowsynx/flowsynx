using FlowSynx.Core.Model;

namespace FlowSynx.Core.Services;

public interface IPluginSpecificationsService
{
    Task<PluginSpecificationsResult> Validate(string type, Dictionary<string, object?>? specifications, 
        CancellationToken cancellationToken);
}