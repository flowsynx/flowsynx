using System.Reflection;
using System.Runtime.Loader;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
            return LoadUnmanagedDllFromPath(libraryPath);

        return nint.Zero;
    }
}