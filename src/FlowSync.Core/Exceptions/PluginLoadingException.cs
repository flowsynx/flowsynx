namespace FlowSync.Core.Exceptions;

public class PluginLoadingException : FlowSyncBaseException
{
    public PluginLoadingException(string message) : base(message) { }
    public PluginLoadingException(string message, Exception inner) : base(message, inner) { }
}