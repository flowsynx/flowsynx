namespace FlowSynx.PluginCore.Exceptions;

public class FlowSynxException : Exception
{
    public int ErrorCode { get; }
    public string ErrorMessage { get; }

    public FlowSynxException(ErrorMessage errorMessage) : this(errorMessage.Code, errorMessage.Message)
    {

    }

    public FlowSynxException(int errorCode, string errorMessage) : base(errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public FlowSynxException(int errorCode, string errorMessage, Exception innerException) : base(errorMessage, innerException)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public override string ToString() => new ErrorMessage(ErrorCode, ErrorMessage).ToString();
}