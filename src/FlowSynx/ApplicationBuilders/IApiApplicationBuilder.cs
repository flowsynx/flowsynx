using FlowSynx.Commands;
using FlowSynx.Core.Services;

namespace FlowSynx.ApplicationBuilders;

public interface IApiApplicationBuilder
{
    Task RunAsync(ILogger logger, int port, CancellationToken cancellationToken);
}