using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class FileReaderException : FlowSyncBaseException
{
    public FileReaderException(string message) : base(message) { }
    public FileReaderException(string message, Exception inner) : base(message, inner) { }
}