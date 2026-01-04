namespace FlowSynx.Infrastructure.Security.Secrets.Exceptions;

public class SecretException : Exception
{
    public SecretException(string message) : base(message) { }
    public SecretException(string message, string errorCode) : base(message) { }
}
