using FlowSync.Enums;

namespace FlowSync.Commands;

public class RootCommandOptions
{
    public required int Port { get; set; }
    public required string Config { get; set; }
    public bool EnableHealthCheck { get; set; }
    public bool EnableLog { get; set; }
    public AppLogLevel AppLogLevel { get; set; }
    public int Retry { get; set; }
}