//namespace FlowSynx.Infrastructure.PluginHost;

//public class PluginResult: IDisposable
//{
//    public bool Succeeded { get; }
//    public PluginHandle? PluginHandle { get; private set; }
//    public List<string> ErrorMessage { get; } = new List<string>();

//    private PluginResult(bool succeeded, string errorMessage)
//    {
//        Succeeded = succeeded;
//        ErrorMessage = new List<string> { errorMessage };
//    }

//    private PluginResult(bool succeeded, PluginHandle? pluginHandle = null)
//    {
//        Succeeded = succeeded;
//        PluginHandle = pluginHandle;
//    }

//    public static PluginResult Success(PluginHandle handle) => new PluginResult(true, handle);
//    public static PluginResult Fail(string message) => new PluginResult(false, message);

//    public void Dispose()
//    {
//        if (PluginHandle != null)
//        {
//            PluginHandle.Dispose();
//            PluginHandle = null;
//        }

//        GC.Collect();
//        GC.WaitForPendingFinalizers();
//        GC.Collect();
//    }
//}