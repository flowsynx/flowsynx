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
            if (IsLoaded) return PluginLoadResult.FromSuccess(_pluginInstance!);

            // Create context for plugin directory
            _context = new PluginLoadContext(Descriptor.DirectoryPath);
            _contextWeakRef = new WeakReference(_context);

            var pluginAssembly = _context.LoadFromAssemblyPath(Descriptor.EntryDllPath);

            var pluginType = pluginAssembly
                .GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t)
                                     && !t.IsAbstract
                                     && t.IsClass);

            if (pluginType is null)
            {
                CleanupAfterFailedLoad();
                return PluginLoadResult.FromFailure("No IPlugin implementation found in plugin folder.");
            }

            // Create instance using parameterless ctor. Consider activating via factory method for more control.
            var ctor = pluginType.GetConstructor(Type.EmptyTypes);
            if (ctor is null)
            {
                CleanupAfterFailedLoad();
                return PluginLoadResult.FromFailure("Plugin type does not have a parameterless constructor.");
            }

            try
            {
                // Create in plugin ALC; cast is allowed only if contract assembly is shared and identity matches.
                var instance = (IPlugin?)Activator.CreateInstance(pluginType);
                if (instance is null)
                {
                    CleanupAfterFailedLoad();
                    return PluginLoadResult.FromFailure("Failed to create plugin instance.");
                }

                _pluginInstance = instance;
                IsLoaded = true;

                return PluginLoadResult.FromSuccess(instance);
            }
            catch (TargetInvocationException tie)
            {
                CleanupAfterFailedLoad();
                return PluginLoadResult.FromFailure($"Plugin ctor threw: {tie.InnerException?.Message ?? tie.Message}");
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
            if (!IsLoaded) return;

            // Allow plugin to dispose/cleanup itself first (if it implements IDisposable internally)
            try
            {
                if (_pluginInstance is IDisposable d)
                {
                    d.Dispose();
                }
            }
            catch { /* swallow plugin cleanup errors */ }

            // Drop the instance reference
            _pluginInstance = null;

            // Drop context reference and trigger unload
            if (_context != null)
            {
                _context.DisposeCollectible();
            }

            // Give the GC some cycles to collect the ALC
            for (int i = 0; _contextWeakRef is not null && _contextWeakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(100, cancellationToken).ContinueWith(_ => { });
            }

            _context = null;
            _contextWeakRef = null;
            IsLoaded = false;
        }
        finally
        {
            _sync.Release();
        }
    }

    public IPlugin? GetPluginInstance() => IsLoaded ? _pluginInstance : null;

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
        catch { }

        _sync.Dispose();
    }
}