using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

public class ConsoleLoggerOptions
{
    public string OutputTemplate { get; set; } = string.Empty;
    public LogLevel MinLevel { get; set; } = LogLevel.Information;
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}