namespace FlowSynx.Domain.TenantSecretConfigs.Logging;

public sealed record TenantFileLoggingPolicy
{
    public string LogLevel { get; init; }
    public string? LogPath { get; init; }
    public string? RollingInterval { get; init; }
    public int? RetainedFileCountLimit { get; init; }

    public static TenantFileLoggingPolicy Create()
    {
        return new TenantFileLoggingPolicy
        {
            LogLevel = "Information",
            LogPath = "logs/",
            RollingInterval = "Day",
            RetainedFileCountLimit = 7
        };
    }
}