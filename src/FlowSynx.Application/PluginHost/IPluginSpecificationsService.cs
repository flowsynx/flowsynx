using FlowSynx.Domain.Plugin;

namespace FlowSynx.Application.PluginHost;

public interface IPluginSpecificationsService
{
    PluginSpecificationsResult Validate(Dictionary<string, object?>? inputSpecifications,
        List<PluginSpecification>? specifications);
}