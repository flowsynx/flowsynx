using System.Globalization;
using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Core.Exceptions;

public class ApiBaseException : FlowSyncBaseException
{
    public ApiBaseException(string message) : base(message) { }
    public ApiBaseException(string message, Exception inner) : base(message, inner) { }
    public ApiBaseException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
}