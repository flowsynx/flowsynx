namespace FlowSynx.Application.Configuration.System.Logger;

public class LoggerProviderConfiguration
{
    public string LogLevel { get; set; } = "Info";
    public string? ConnectionString { get; set; } = $"Data Source=flowsynx-logs.db";
    public string? FilePath { get; set; } = "logs/flowsynx.log";
    public string? RollingInterval { get; set; } = "Day";
    public int? RetainedFileCountLimit { get; set; } = 7;
    public string? Url { get; set; }
    public string? IndexFormat { get; set; }
    public string? ApiKey { get; set; }
    public int? Port { get; set; }
    public string? Region { get; set; }
    public string? LogGroup { get; set; }
    public string? LogStream { get; set; }
    public string? ProjectId { get; set; }
    public string? LogName { get; set; }
    public string? WorkspaceId { get; set; }
    public string? SharedKey { get; set; }
}