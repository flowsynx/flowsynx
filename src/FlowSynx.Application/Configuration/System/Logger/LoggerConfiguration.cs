using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Configuration.System.Logger;

public class LoggerConfiguration
{
    public string Level { get; set; } = nameof(LogsLevel.Info);
}