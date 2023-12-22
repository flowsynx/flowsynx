using FlowSynx.Commands;

namespace FlowSynx.ApplicationBuilders;

public interface IApiApplicationBuilder
{
    Task RunAsync(RootCommandOptions rootCommandOptions);
}