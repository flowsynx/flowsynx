using FlowSynx.Application.Models;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;
using System.Reflection;
using System.Runtime.Loader;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginLoader: IDisposable// : IPluginLoader
{
    private AssemblyLoadContext _context;
    private WeakReference _contextWeakRef;
    private IPlugin _pluginInstance;
    private bool _isUnloaded;

    public IPlugin Plugin => _pluginInstance;
    public bool IsUnloaded => _isUnloaded;

    public PluginLoader(string pluginLocation)
    {
        if (!File.Exists(pluginLocation))
            throw new FlowSynxException((int)ErrorCode.PluginNotFound, 
                string.Format(Resources.Plugin_Loader_FileNotFound, pluginLocation));

        _context = new PluginLoadContext(pluginLocation);
        _contextWeakRef = new WeakReference(_context);

        Assembly pluginAssembly = _context.LoadFromAssemblyPath(pluginLocation);

        var interfaceType = typeof(IPlugin);
        var types = pluginAssembly.GetTypes();

        var pluginType = pluginAssembly
            .GetTypes()
            .FirstOrDefault(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        if (pluginType == null)
        {
            Unload();
            throw new FlowSynxException((int)ErrorCode.PluginLoader,
                string.Format(Resources.Plugin_Loader_FileNotFound, pluginLocation));
        }

        _pluginInstance = (IPlugin)Activator.CreateInstance(pluginType)!;
    }


    public void Unload()
    {
        if (_isUnloaded) 
            return;

        _pluginInstance = null!;
        _context.Unload();
        _context = null!;
        _isUnloaded = true;

        for (int i = 0; _contextWeakRef.IsAlive && i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public void Dispose()
    {
        Unload();
    }

    //public PluginHandle LoadPlugin(string pluginLocation)
    //{
    //    return GetImplementationsOfInterface(pluginLocation);
    //}

    //private PluginHandle GetPluginInstance(string pluginLocation)
    //{
    //    if (!File.Exists(pluginLocation))
    //        return PluginResult.Fail(string.Format(Resources.Plugin_Loader_FileNotFound, pluginLocation));

    //    using (var context = new PluginLoadContext(pluginLocation))
    //    {
    //        try
    //        {
    //            Assembly pluginAssembly = context.LoadFromAssemblyPath(pluginLocation);

    //            var interfaceType = typeof(IPlugin);
    //            var types = pluginAssembly.GetTypes();

    //            var pluginType = pluginAssembly
    //                .GetTypes()
    //                .FirstOrDefault(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    //            if (pluginType == null)
    //                return PluginResult.Fail(string.Format(Resources.Plugin_Loader_FileNotFound, pluginLocation));

    //            var pluginInstance = (IPlugin)Activator.CreateInstance(pluginType)!;
    //            return PluginResult.Success(new PluginHandle(pluginInstance));
    //        }
    //        catch (Exception ex)
    //        {
    //            return PluginResult.Fail($"Error loading plugin: {ex.Message}");
    //        }
    //    }
    //}

    //private PluginHandle GetImplementationsOfInterface(string pluginLocation)
    //{
    //    if (!File.Exists(pluginLocation))
    //        return PluginHandle.Fail(string.Format(Resources.Plugin_Loader_FileNotFound, pluginLocation));

    //    try
    //    {
    //        IPlugin? pluginInstance = null;
    //        var context = new PluginLoadContext(pluginLocation);

    //        Assembly pluginAssembly = context.LoadFromAssemblyPath(pluginLocation);

    //        var interfaceType = typeof(IPlugin);
    //        var types = pluginAssembly.GetTypes();

    //        var pluginType = pluginAssembly
    //            .GetTypes()
    //            .FirstOrDefault(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    //        if (pluginType == null)
    //            return PluginHandle.Fail(string.Format(Resources.Plugin_Loader_FileNotFound, pluginLocation));

    //        pluginInstance = (IPlugin)Activator.CreateInstance(pluginType)!;

    //        return PluginHandle.Success(pluginInstance);
    //    }
    //    catch (Exception ex)
    //    {
    //        return PluginHandle.Fail($"Error loading plugin: {ex.Message}");
    //    }
    //}
}