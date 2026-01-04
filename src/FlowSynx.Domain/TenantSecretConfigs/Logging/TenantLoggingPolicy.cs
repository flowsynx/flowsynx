namespace FlowSynx.Domain.TenantSecretConfigs.Logging;

public sealed record TenantLoggingPolicy
{
    public bool Enabled { get; init; } = false;
    public TenantFileLoggingPolicy File { get; init; } = new();
    public TenantSeqLoggingPolicy Seq { get; set; } = new();

    public static TenantLoggingPolicy Create()
    {
        return new TenantLoggingPolicy
        {
            Enabled = false,
            File = TenantFileLoggingPolicy.Create(),
            Seq = TenantSeqLoggingPolicy.Create()
        };
    }
}