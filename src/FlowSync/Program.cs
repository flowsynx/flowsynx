using System.CommandLine;
using System.CommandLine.IO;
using FlowSync.Extensions;
using FlowSync.Enums;
using FlowSync.Commands;
using FlowSync.Infrastructure.Extensions;
using FlowSync.Models;
using FlowSync.Services;
using FlowSync.ApplicationBuilders;

IServiceCollection serviceCollection = new ServiceCollection()
    .AddLoggingService(true, AppLogLevel.All)
    .AddFlowSyncInfrastructure()
    .AddTransient<RootCommand, Root>()
    .AddTransient<IOptionsVerifier, OptionsVerifier>()
    .AddTransient<IApiApplicationBuilder, ApiApplicationBuilder>()
    .AddTransient<ICliApplicationBuilder, CliApplicationBuilder>();

IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

try
{
    var cli = serviceProvider.GetService<ICliApplicationBuilder>();

    if (cli == null)
        throw new Exception("Something wrong happen during execute the application");

    return await cli.RunAsync(args);
}
catch (Exception ex)
{
    var logger = serviceProvider.GetService<ILogger<Program>>();
    if (logger != null)
        logger.LogError(ex.Message);
    else
        Console.Error.WriteLine(ex.Message);

    return ExitCode.Error;
}