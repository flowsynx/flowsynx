using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginHandle
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IPlugin Instance { get; private set; } = default!;
    public string Location { get; private set; } = default!;

    public static PluginHandle Ok(IPlugin pluginInstance, string location) => new PluginHandle
    {
        Success = true,
        Message = "Plugin loaded successfully.",
        Instance = pluginInstance,
        Location = location
    };

    public static PluginHandle Fail(string message) => new PluginHandle
    {
        Success = false,
        Message = message
    };
}
