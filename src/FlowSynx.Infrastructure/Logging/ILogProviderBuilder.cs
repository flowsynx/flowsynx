using FlowSynx.Infrastructure.Configuration.System.Logger;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;


public interface ILogProviderBuilder
{
    ILoggerProvider? Build(string name, LoggerProviderConfiguration? config);
}