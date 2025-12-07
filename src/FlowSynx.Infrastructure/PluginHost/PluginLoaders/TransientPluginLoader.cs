using System.Reflection;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.PluginHost.PluginLoaders;

public sealed class TransientPluginLoader : IPluginLoader, IDisposable
{
    private readonly object _sync = new();
    private readonly string _pluginLocation;

    private WeakReference? _contextWeakRef;
    private IPlugin? _pluginInstance;
    private bool _isUnloaded;
    private bool _isLoaded;

    public IPlugin GetPlugin()
    {
        return !_isLoaded || _isUnloaded || _pluginInstance is null
            ? throw new ObjectDisposedException(nameof(TransientPluginLoader))
            : _pluginInstance;
    }

    public bool IsLoaded => _isLoaded && !_isUnloaded && _pluginInstance is not null;

    public TransientPluginLoader(string pluginLocation)
    {
        ValidatePluginFile(pluginLocation);
        _pluginLocation = pluginLocation;
    }

    public void Load()
    {
        lock (_sync)
        {
            if (_isLoaded && !_isUnloaded)
                return; // Idempotent load

            try
            {
                using var context = new PluginLoadContext(_pluginLocation);
                _contextWeakRef = new WeakReference(context, trackResurrection: true);

                var pluginAssembly = context.LoadFromAssemblyPath(_pluginLocation);
                var pluginType = LoadPluginType(pluginAssembly);

                if (pluginType is null)
                {
                    throw new FlowSynxException((int)ErrorCode.PluginLoader,
                        Localization.Get("Plugin_Loader_NoPluginFound"));
                }

                _pluginInstance = CreatePluginInstance(pluginType);
                _isLoaded = true;
                _isUnloaded = false;
            }
            catch (FlowSynxException)
            {
                // Preserve domain-specific exceptions
                CleanupAfterFailedLoad();
                throw;
            }
            catch (Exception ex)
            {
                CleanupAfterFailedLoad();
                throw new FlowSynxException((int)ErrorCode.PluginLoader,
                    Localization.Get("Plugin_Loader_LoadFailed", ex.Message), ex);
            }
        }
    }

    public void Unload()
    {
        lock (_sync)
        {
            if (_isUnloaded)
                return;

            SafeUnload();
        }
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    private void ValidatePluginFile(string pluginLocation)
    {
        if (!File.Exists(pluginLocation))
        {
            throw new FlowSynxException((int)ErrorCode.PluginNotFound,
                Localization.Get("Plugin_Loader_FileNotFound", pluginLocation));
        }

        // Basic sanity check to avoid loading non-assemblies
        if (!pluginLocation.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                Localization.Get("Plugin_Loader_InvalidExtension", pluginLocation));
        }
    }

    private static Type? LoadPluginType(Assembly pluginAssembly)
    {
        var interfaceType = typeof(IPlugin);
        // Prefer a single concrete type; if multiple, pick the first to keep behavior deterministic.
        return pluginAssembly
            .GetTypes()
            .FirstOrDefault(t =>
                interfaceType.IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract);
    }

    private static IPlugin CreatePluginInstance(Type pluginType)
    {
        // Require parameterless ctor for transient load
        var ctor = pluginType.GetConstructor(Type.EmptyTypes);
        if (ctor is null)
        {
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                Localization.Get("Plugin_Loader_MissingDefaultCtor", pluginType.FullName ?? pluginType.Name));
        }

        try
        {
            if (Activator.CreateInstance(pluginType) is not IPlugin instance)
            {
                throw new FlowSynxException((int)ErrorCode.PluginLoader,
                    Localization.Get("Plugin_Loader_FailedToCreateInstance", pluginType.FullName ?? pluginType.Name));
            }

            return instance;
        }
        catch (TargetInvocationException tie)
        {
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                Localization.Get("Plugin_Loader_ConstructorException", tie.InnerException?.Message ?? tie.Message), tie.InnerException ?? tie);
        }
        catch (Exception ex)
        {
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                Localization.Get("Plugin_Loader_FailedToCreateInstance", pluginType.FullName ?? pluginType.Name), ex);
        }
    }

    private void SafeUnload()
    {
        try
        {
            // Null out plugin references first to allow GC
            _pluginInstance = null;

            // Try to unload the load context explicitly if still reachable
            if (_contextWeakRef?.Target is PluginLoadContext ctx)
            {
                try
                {
                    ctx.Unload();
                }
                catch
                {
                    // Do not propagate unload exceptions
                }
            }

            // Encourage unloading of collectible ALC
            for (var i = 0; _contextWeakRef is not null && _contextWeakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        catch
        {
            // Swallow unload exceptions to not mask original issues
        }
        finally
        {
            _isUnloaded = true;
            _isLoaded = false;
            _contextWeakRef = null;
        }
    }

    private void CleanupAfterFailedLoad()
    {
        _pluginInstance = null;
        _isLoaded = false;
        _isUnloaded = false;
        _contextWeakRef = null;
    }
}