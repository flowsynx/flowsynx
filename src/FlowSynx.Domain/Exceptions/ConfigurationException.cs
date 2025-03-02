namespace FlowSynx.Domain.Exceptions;

public class ConfigurationException : BaseException
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception inner) : base(message, inner) { }
}