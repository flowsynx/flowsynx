using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginTypeConfiguration
{
    public string? Plugin { get; set; }
    public string? Version { get; set; }
    public PluginSpecifications? Specifications { get; set; }
}