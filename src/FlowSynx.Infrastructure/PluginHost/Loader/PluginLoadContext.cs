using System.Reflection;
using System.Runtime.Loader;

namespace FlowSynx.Infrastructure.PluginHost.Loader;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public string PluginDirectory { get; }

    public PluginLoadContext(string pluginDirectory)
    : base(name: Path.GetFileName(pluginDirectory), isCollectible: true)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
            throw new ArgumentNullException(nameof(pluginDirectory));

        if (!Directory.Exists(pluginDirectory))
            throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDirectory}");

        PluginDirectory = pluginDirectory;
        _resolver = new AssemblyDependencyResolver(pluginDirectory);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        return null; // fallback to default
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }

    /// <summary>
    /// Best-effort unload wrapper; caller should drop all references to plugin types before calling.
    /// </summary>
    public void DisposeCollectible()
    {
        try
        {
            Unload();
        }
        catch (Exception ex)
        {
            // Keep this non-fatal in production. Log and continue.
            Console.Error.WriteLine($"PluginLoadContext.Unload error: {ex}");
        }
    }
}