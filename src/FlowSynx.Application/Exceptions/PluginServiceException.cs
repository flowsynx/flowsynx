using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Application.Exceptions;

public class PluginServiceException : BaseException
{
    public PluginServiceException(string message) : base(message) { }
    public PluginServiceException(string message, Exception inner) : base(message, inner) { }
}