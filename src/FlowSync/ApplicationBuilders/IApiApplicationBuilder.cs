using FlowSync.Commands;

namespace FlowSync.ApplicationBuilders;

public interface IApiApplicationBuilder
{
    Task RunAsync(RootCommandOptions rootCommandOptions);
}