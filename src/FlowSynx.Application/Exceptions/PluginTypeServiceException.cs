using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Application.Exceptions;

public class PluginTypeServiceException : BaseException
{
    public PluginTypeServiceException(string message) : base(message) { }
    public PluginTypeServiceException(string message, Exception inner) : base(message, inner) { }
}