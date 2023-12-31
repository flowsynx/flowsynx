using System.CommandLine;
using FlowSynx;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Commands;
using FlowSynx.Environment;
using FlowSynx.Extensions;
using FlowSynx.IO;
using FlowSynx.Logging;
using FlowSynx.Models;
using FlowSynx.Services;

IServiceCollection serviceCollection = new ServiceCollection()
    .AddLocation()
    .AddSerialization()
    .AddEnvironmentManager()
    .AddEndpoint()
    .AddLoggingService()
    .AddTransient<RootCommand, Root>()
    .AddTransient<IOptionsVerifier, OptionsVerifier>()
    .AddTransient<IApiApplicationBuilder, ApiApplicationBuilder>()
    .AddTransient<ICliApplicationBuilder, CliApplicationBuilder>();

IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

try
{
    var cli = serviceProvider.GetService<ICliApplicationBuilder>();

    if (cli == null)
        throw new Exception(Resources.EntryPointErrorInExecutteApplication);

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