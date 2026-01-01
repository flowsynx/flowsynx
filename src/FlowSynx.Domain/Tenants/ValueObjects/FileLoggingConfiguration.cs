namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record FileLoggingConfiguration
{
    public string LogLevel { get; init; }
    public string? LogPath { get; init; }
    public string? RollingInterval { get; init; }
    public int? RetainedFileCountLimit { get; init; }

    public static FileLoggingConfiguration Create()
    {
        return new FileLoggingConfiguration
        {
            LogLevel = "Information",
            LogPath = "logs/",
            RollingInterval = "Day",
            RetainedFileCountLimit = 7
        };
    }
}