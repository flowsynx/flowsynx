using EnsureThat;
using FlowSync.Abstractions;

namespace FlowSync.Infrastructure.Extensions;

internal static class PluginExtensions
{
    public static T CastTo<T>(this IPlugin plugin) where T: IPlugin
    {
        EnsureArg.IsNotNull(plugin, nameof(plugin));
        return (T)plugin;
    }
}