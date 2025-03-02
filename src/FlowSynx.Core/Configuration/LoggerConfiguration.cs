using FlowSynx.Domain.Entities.Logs;

namespace FlowSynx.Core.Configuration;

public class LoggerConfiguration
{
    public string Level { get; set; } = LogsLevel.Info.ToString();
}