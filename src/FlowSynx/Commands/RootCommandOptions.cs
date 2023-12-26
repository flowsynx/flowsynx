namespace FlowSynx.Commands;

public class RootCommandOptions
{
    public required string ConfigFile { get; set; }
    public bool EnableHealthCheck { get; set; }
    public bool EnableLog { get; set; }
    public AppLogLevel LogLevel { get; set; }
    public int Retry { get; set; }
}