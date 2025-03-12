using FlowSynx.Commands;
using FlowSynx.Application.Services;

namespace FlowSynx.ApplicationBuilders;

public interface IApiApplicationBuilder
{
    Task RunAsync(ILogger logger, int port, CancellationToken cancellationToken);
}