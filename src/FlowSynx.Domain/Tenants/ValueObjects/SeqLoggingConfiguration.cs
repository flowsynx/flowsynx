namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record SeqLoggingConfiguration
{
    public string LogLevel { get; init; }
    public string? ApiKey { get; init; }
    public string? Url { get; init; }

    public static SeqLoggingConfiguration Create()
    {
        return new SeqLoggingConfiguration
        {
            LogLevel = "Information"
        };
    }
}