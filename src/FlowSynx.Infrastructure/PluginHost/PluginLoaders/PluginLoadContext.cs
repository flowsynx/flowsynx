using System.Reflection;
using System.Runtime.Loader;

namespace FlowSynx.Infrastructure.PluginHost.PluginLoaders;

public class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: true), IDisposable
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);
    private bool _disposed;

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var defaultAssembly = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        if (defaultAssembly != null)
            return defaultAssembly;

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : nint.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Unload();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}