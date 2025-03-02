using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Connectors.Exceptions;

public class ConnectorsException : BaseException
{
    public ConnectorsException(string message) : base(message) { }
    public ConnectorsException(string message, Exception inner) : base(message, inner) { }
}