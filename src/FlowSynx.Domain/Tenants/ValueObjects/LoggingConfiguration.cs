namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record LoggingConfiguration
{
    public bool Enabled { get; init; } = false;
    public FileLoggingConfiguration File { get; init; } = new();
    public SeqLoggingConfiguration Seq { get; set; } = new();

    public static LoggingConfiguration Create()
    {
        return new LoggingConfiguration
        {
            Enabled = false,
            File = FileLoggingConfiguration.Create(),
            Seq = SeqLoggingConfiguration.Create()
        };
    }
}