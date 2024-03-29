using System.CommandLine;
using FlowSynx;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Commands;
using FlowSynx.Environment;
using FlowSynx.Extensions;
using FlowSynx.IO;
using FlowSynx.Models;
using FlowSynx.Services;

IHost host = GetHost();
IServiceProvider serviceProvider = host.Services;

try
{
    var cli = serviceProvider.GetRequiredService<ICliApplicationBuilder>();

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

IHost GetHost()
{
    var hostBuilder = new HostBuilder().ConfigureServices(services =>
    {
        services.AddLocation()
                .AddSerialization()
                .AddEndpoint()
                .AddLoggingService()
                .AddTransient<RootCommand, Root>()
                .AddTransient<IOptionsVerifier, OptionsVerifier>()
                .AddTransient<IApiApplicationBuilder, ApiApplicationBuilder>()
                .AddTransient<ICliApplicationBuilder, CliApplicationBuilder>();
    });
    
    return hostBuilder.UseConsoleLifetime().Build();
}