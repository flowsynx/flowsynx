//using FlowSynx.PluginCore;

//namespace FlowSynx.Infrastructure.PluginHost;

//public class PluginHandle : IDisposable
//{
//    public bool Succeeded { get; }
//    public IPlugin? PluginInstance { get; set; }
//    public List<string> ErrorMessage { get; } = new List<string>();

//    private PluginHandle(bool succeeded, string errorMessage)
//    {
//        Succeeded = succeeded;
//        ErrorMessage = new List<string> { errorMessage };
//    }

//    private PluginHandle(bool succeeded, IPlugin? pluginInstance = null)
//    {
//        Succeeded = succeeded;
//        PluginInstance = pluginInstance;
//    }

//    public static PluginHandle Success(IPlugin pluginInstance) => new PluginHandle(true, pluginInstance);
//    public static PluginHandle Fail(string message) => new PluginHandle(false, message);








//    ////public PluginLoadContext? LoadContext { get; set; }
//    //public IPlugin? PluginInstance { get; set; }

//    ////public PluginHandle(IPlugin pluginInstance) : this(pluginInstance, null) { }
//    //public PluginHandle(IPlugin pluginInstance) 
//    //{
//    //    PluginInstance = pluginInstance;
//    //}

//    ////public PluginHandle(IPlugin pluginInstance, PluginLoadContext? loadContext)
//    ////{
//    ////    //LoadContext = loadContext;
//    ////    PluginInstance = pluginInstance;
//    ////}

//    public void Dispose()
//    {
//        //LoadContext?.Dispose();
//        //LoadContext = null;
//        PluginInstance = null;

//        GC.Collect();
//        GC.WaitForPendingFinalizers();
//        GC.Collect();
//    }
//}