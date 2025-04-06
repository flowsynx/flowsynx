using FlowSynx.Domain.Entities.Plugin;
using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public interface IExtractPluginSpecifications
{
    List<PluginSpecification> GetPluginSpecification(IPlugin? plugin);
}