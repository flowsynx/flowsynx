using System.Globalization;

namespace FlowSync.Core.Exceptions;

public class ApiException : Exception
{
    public ApiException() { }
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception inner) : base(message, inner) { }
    public ApiException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
}