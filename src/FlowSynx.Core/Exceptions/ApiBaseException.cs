using System.Globalization;
using FlowSynx.Abstractions.Exceptions;

namespace FlowSynx.Core.Exceptions;

public class ApiBaseException : FlowSynxException
{
    public ApiBaseException(string message) : base(message) { }
    public ApiBaseException(string message, Exception inner) : base(message, inner) { }
    public ApiBaseException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
}