using FlowSync.Commands;

namespace FlowSync.ApplicationBuilders;

public interface IApiApplicationBuilder
{
    Task RunAsync(CommandOptions commandOptions);
}