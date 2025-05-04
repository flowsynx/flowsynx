using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Configuration;

public class LoggerConfiguration
{
    public string Level { get; set; } = nameof(LogsLevel.Info);
}