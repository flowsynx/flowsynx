using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost.Loader;

public sealed class PluginLoadResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public IPlugin? PluginInstance { get; }

    private PluginLoadResult(bool success, IPlugin? pluginInstance, string? errorMessage)
    {
        Success = success;
        PluginInstance = pluginInstance;
        ErrorMessage = errorMessage;
    }

    public static PluginLoadResult FromSuccess(IPlugin instance) => new(true, instance, null);
    public static PluginLoadResult FromFailure(string message) => new(false, null, message);
}