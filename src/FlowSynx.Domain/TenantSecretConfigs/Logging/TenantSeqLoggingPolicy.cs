namespace FlowSynx.Domain.TenantSecretConfigs.Logging;

public sealed record TenantSeqLoggingPolicy
{
    public string LogLevel { get; init; }
    public string? ApiKey { get; init; }
    public string? Url { get; init; }

    public static TenantSeqLoggingPolicy Create()
    {
        return new TenantSeqLoggingPolicy
        {
            LogLevel = "Information"
        };
    }
}