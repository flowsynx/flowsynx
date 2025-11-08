using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Configuration.Logger;

public class LoggerConfiguration
{
    public string Level { get; set; } = nameof(LogsLevel.Info);
}