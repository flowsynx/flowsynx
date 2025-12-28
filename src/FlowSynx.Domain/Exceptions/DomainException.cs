namespace FlowSynx.Domain.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }
    public string? AdditionalInfo { get; }

    public DomainException(string message) : base(message)
    {
        ErrorCode = "DOMAIN_ERROR";
    }

    public DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, string errorCode, string additionalInfo) : base(message)
    {
        ErrorCode = errorCode;
        AdditionalInfo = additionalInfo;
    }

    public DomainException(string message, System.Exception innerException) : base(message, innerException)
    {
        ErrorCode = "DOMAIN_ERROR";
    }
}
