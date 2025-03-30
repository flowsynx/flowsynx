using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using FlowSynx.Models;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.ApplicationBuilders;

public class CliApplicationBuilder : ICliApplicationBuilder
{
    private readonly ILogger<CliApplicationBuilder> _logger;
    private readonly RootCommand _rootCommand;

    public CliApplicationBuilder(ILogger<CliApplicationBuilder> logger, RootCommand rootCommand)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(rootCommand);

        _logger = logger;
        _rootCommand = rootCommand;
    }

    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            var commandLineBuilder = new CommandLineBuilder(_rootCommand)
                .UseHelp()
                .UseEnvironmentVariableDirective()
                .UseParseDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination();

            var parser = commandLineBuilder.Build();
            return await parser.InvokeAsync(args);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return ExitCode.Error;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return ExitCode.Error;
        }
    }
}