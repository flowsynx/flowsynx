using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class FileWriterException : FlowSyncBaseException
{
    public FileWriterException(string message) : base(message) { }
    public FileWriterException(string message, Exception inner) : base(message, inner) { }
}