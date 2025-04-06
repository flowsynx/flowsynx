using FlowSynx.Application.Model;
using FlowSynx.Domain.Entities.Plugin;

namespace FlowSynx.Application.Services;

public interface IPluginSpecificationsService
{
    PluginSpecificationsResult Validate(Dictionary<string, object?> inputSpecifications, 
        List<PluginSpecification> specifications);
}