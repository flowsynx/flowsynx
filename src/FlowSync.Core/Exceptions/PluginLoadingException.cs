namespace FlowSync.Core.Exceptions;

public class PluginLoadingException : Exception
{
    public PluginLoadingException() { }
    public PluginLoadingException(string message) : base(message) { }
    public PluginLoadingException(string message, Exception inner) : base(message, inner) { }
}