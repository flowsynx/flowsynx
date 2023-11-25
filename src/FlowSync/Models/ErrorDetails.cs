using FlowSync.Core.Serialization;

namespace FlowSync.Models;

public class ErrorDetails
{
    private readonly ISerializer _serializer;

    public ErrorDetails(ISerializer serializer, string message, int statusCode)
    {
        _serializer = serializer;
        Message = message;
        StatusCode = statusCode;
    }

    public int StatusCode { get; set; }
    public string Message { get; set; }

    public override string ToString()
    {
        return _serializer.Serialize(this);
    }
}