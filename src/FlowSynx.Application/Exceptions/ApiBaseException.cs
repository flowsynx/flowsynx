using System.Globalization;
using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Application.Exceptions;

public class ApiBaseException : BaseException
{
    public ApiBaseException(string message) : base(message) { }
    public ApiBaseException(string message, Exception inner) : base(message, inner) { }
    public ApiBaseException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
}