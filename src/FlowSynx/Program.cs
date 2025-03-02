using System.CommandLine;
using FlowSynx;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Commands;
using FlowSynx.Extensions;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Models;
using FlowSynx.Persistence.SQLite.Extensions;

var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;
IHost host = GetHost(cancellationToken);
IServiceProvider serviceProvider = host.Services;

try
{
    serviceProvider.EnsureLogDatabaseCreated();
    var cli = serviceProvider.GetRequiredService<ICliApplicationBuilder>() ?? 
              throw new Exception(Resources.EntryPointErrorInExecutteApplication);

    Console.CancelKeyPress += (sender, eventArgs) =>
    {
        Console.WriteLine("Cancellation requested.");
        cts.Cancel();
        eventArgs.Cancel = true; // Don't terminate immediately
    };

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

IHost GetHost(CancellationToken cancellationToken)
{
    var hostBuilder = new HostBuilder().ConfigureServices((hostContext, services) =>
    {
        IConfiguration config = hostContext.Configuration;

        services.AddHttpContextAccessor()
                .AddSQLiteLoggerLayer()
                .AddLocation()
                .AddJsonSerialization()
                .AddEndpoint()
                .AddUserService()
                .AddLoggingService(config, cancellationToken)
                .AddScoped<RootCommand, Root>()
                .AddScoped<IApiApplicationBuilder, ApiApplicationBuilder>()
                .AddScoped<ICliApplicationBuilder, CliApplicationBuilder>();
    });
    
    return hostBuilder.UseConsoleLifetime().Build();
}