using FlowSynx.PluginCore;
using System.Reflection;

namespace FlowSynx.Infrastructure.PluginHost.Loader;

public sealed class PluginLoader : IDisposable
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private PluginLoadContext? _context;
    private WeakReference? _contextWeakRef;
    private IPlugin? _pluginInstance;
    private bool _disposed;

    /// <summary>
    /// Descriptor does not load assemblies; safe to keep strongly.
    /// </summary>
    public PluginDescriptor Descriptor { get; }

    public bool IsLoaded { get; private set; }

    public PluginLoader(string pluginEntryDllPath)
    {
        Descriptor = new PluginDescriptor(pluginEntryDllPath);
    }

    public async Task<PluginLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (IsLoaded)
                return PluginLoadResult.FromSuccess(_pluginInstance!);

            // Create unloadable ALC for this plugin instance
            _context = new PluginLoadContext(Descriptor.DirectoryPath);
            _contextWeakRef = new WeakReference(_context);

            // Load entry assembly inside the plugin ALC
            var pluginAssembly = _context.LoadFromAssemblyPath(Descriptor.EntryDllPath);

            // Find IPlugin implementation
            var pluginType = pluginAssembly
                .GetTypes()
                .FirstOrDefault(t =>
                    typeof(IPlugin).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && t.IsClass);

            if (pluginType is null)
            {
                CleanupAfterFailedLoad();
                return PluginLoadResult.FromFailure("No IPlugin implementation found in plugin.");
            }

            // Must have parameterless ctor
            if (pluginType.GetConstructor(Type.EmptyTypes) is not ConstructorInfo ctor)
            {
                CleanupAfterFailedLoad();
                return PluginLoadResult.FromFailure("IPlugin implementation must have a parameterless constructor.");
            }

            try
            {
                // Activator.CreateInstance executed inside plugin ALC
                var instance = (IPlugin?)ctor.Invoke(null);

                if (instance is null)
                {
                    CleanupAfterFailedLoad();
                    return PluginLoadResult.FromFailure("Failed to instantiate plugin.");
                }

                _pluginInstance = instance;
                IsLoaded = true;

                return PluginLoadResult.FromSuccess(instance);
            }
            catch (TargetInvocationException tie)
            {
                CleanupAfterFailedLoad();
                return PluginLoadResult.FromFailure(
                    $"Plugin threw during construction: {tie.InnerException?.Message ?? tie.Message}");
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!IsLoaded)
                return;

            // Allow plugin to perform internal cleanup
            try
            {
                if (_pluginInstance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
                // Swallow plugin cleanup exceptions
            }

            // IMPORTANT: Drop strong references to plugin instance first
            _pluginInstance = null;

            // Request ALC unload
            if (_context != null)
            {
                _context.Unload();
            }

            // Wait for the ALC to be collected naturally by runtime
            if (_contextWeakRef != null)
            {
                // ~5 seconds total (50 × 100ms)
                for (int i = 0; i < 50 && _contextWeakRef.IsAlive; i++)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            // Hard release references
            _context = null;
            _contextWeakRef = null;

            IsLoaded = false;
        }
        finally
        {
            _sync.Release();
        }
    }

    public IPlugin? GetPluginInstance() =>
        IsLoaded ? _pluginInstance : null;

    private void CleanupAfterFailedLoad()
    {
        _pluginInstance = null;
        _context = null;
        _contextWeakRef = null;
        IsLoaded = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            UnloadAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }

        _sync.Dispose();
    }
}