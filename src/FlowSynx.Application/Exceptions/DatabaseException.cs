using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Application.Exceptions;

public class DatabaseException : BaseException
{
    public DatabaseException(string message) : base(message) { }
    public DatabaseException(string message, Exception inner) : base(message, inner) { }
}