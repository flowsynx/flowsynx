using FlowSynx.PluginCore;
using System.Reflection;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginHandle
{
    public IPlugin Instance { get; set; } = default!;
    public PluginLoadContext LoadContext { get; set; } = default!;
    public Assembly Assembly { get; set; } = default!;
    public string Path { get; set; } = default!;
}