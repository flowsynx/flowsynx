using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

public class DatabaseLoggerOptions
{
    public LogLevel MinLevel { get; set; } = LogLevel.Information;
    public CancellationToken CancellationToken { get; set; } = new CancellationToken();
}