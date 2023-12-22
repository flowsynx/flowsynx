using EnsureThat;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Extensions;

internal static class PluginExtensions
{
    public static T CastTo<T>(this IPlugin plugin) where T: IPlugin
    {
        EnsureArg.IsNotNull(plugin, nameof(plugin));
        return (T)plugin;
    }
}