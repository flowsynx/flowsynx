using FlowSynx.Application.Configuration.System.Logger;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public interface ILoggingProviderFactory
{
    ILoggerProvider? Create(string name, LoggerProviderConfiguration config);
}