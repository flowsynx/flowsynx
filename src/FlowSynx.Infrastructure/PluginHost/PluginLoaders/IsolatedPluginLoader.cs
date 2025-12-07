using FlowSynx.Domain;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.PluginCore;
using System.Reflection;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Infrastructure.PluginHost.PluginLoaders;

public class IsolatedPluginLoader : IPluginLoader
{
    private readonly string _pluginLocation;
    private PluginLoadContext? _context;
    private WeakReference? _contextWeakRef;
    private IPlugin? _pluginInstance;
    private bool _isUnloaded;

    public IPlugin GetPlugin() => _pluginInstance ?? throw new ObjectDisposedException(nameof(IsolatedPluginLoader));

    public IsolatedPluginLoader(string pluginLocation)
    {
        ValidatePluginFile(pluginLocation);
        _pluginLocation = pluginLocation;
    }

    public void Load()
    {
        _context = new PluginLoadContext(_pluginLocation);
        _contextWeakRef = new WeakReference(_context, true);

        var pluginAssembly = _context.LoadFromAssemblyPath(_pluginLocation);
        var pluginType = LoadPluginType(pluginAssembly);

        if (pluginType == null)
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                Localization.Get("Plugin_Loader_NoPluginFound"));

        _pluginInstance = CreatePluginInstance(pluginType);
    }

    private void ValidatePluginFile(string pluginLocation)
    {
        if (!File.Exists(pluginLocation))
        {
            throw new FlowSynxException((int)ErrorCode.PluginNotFound,
                Localization.Get("Plugin_Loader_FileNotFound", pluginLocation));
        }
    }

    private Type? LoadPluginType(Assembly pluginAssembly)
    {
        var interfaceType = typeof(IPlugin);
        return pluginAssembly
            .GetTypes()
            .FirstOrDefault(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
    }

    private IPlugin CreatePluginInstance(Type pluginType)
    {
        if (Activator.CreateInstance(pluginType) is not IPlugin instance)
        {
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                Localization.Get("Plugin_Loader_FailedToCreateInstance", pluginType.FullName));
        }
        return instance;
    }

    public void Unload()
    {
        if (_isUnloaded)
            return;

        SafeUnload();
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    private void SafeUnload()
    {
        try
        {
            _pluginInstance = null;
            _context?.Unload();
            _context = null;
            _isUnloaded = true;

            if (_contextWeakRef == null) 
                return;

            for (var i = 0; _contextWeakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        catch
        {
            // Swallow unload exceptions to not mask the original exception
        }
    }
}