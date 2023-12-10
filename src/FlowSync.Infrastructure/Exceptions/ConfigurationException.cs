using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class ConfigurationException : FlowSyncBaseException
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception inner) : base(message, inner) { }
}