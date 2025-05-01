using System.Reflection;
using System.Runtime.Loader;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginLoadContext : AssemblyLoadContext//, IDisposable
{
    private readonly AssemblyDependencyResolver _resolver;
    private bool _disposed = false;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var defaultAssembly = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        if (defaultAssembly != null)
            return defaultAssembly;

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }

    //public void Dispose()
    //{
    //    if (!_disposed)
    //    {
    //        _disposed = true;

    //        Unload();

    //        GC.Collect();
    //        GC.WaitForPendingFinalizers();
    //        GC.Collect();
    //    }
    //}

    //public new void Unload()
    //{
    //    // Explicitly unload the context and trigger finalization
    //    base.Unload();
    //}
}