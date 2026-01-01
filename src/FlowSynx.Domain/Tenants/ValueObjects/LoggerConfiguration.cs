namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record LoggerConfiguration
{
    public bool Enabled { get; init; } = false;
    public FileLoggerConfiguration File { get; init; } = new();

    public static LoggerConfiguration Create()
    {
        return new LoggerConfiguration
        {
            Enabled = false,
            File = FileLoggerConfiguration.Create()
        };
    }
}