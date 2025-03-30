namespace FlowSynx.PluginCore.Exceptions;

public class ErrorMessage
{
    public int Code { get; }
    public string Message { get; }

    public ErrorMessage(int code, string message)
    {
        Code = code;
        Message = message;
    }

    public override string ToString() => $"[FX{Code}] {Message}";
}
