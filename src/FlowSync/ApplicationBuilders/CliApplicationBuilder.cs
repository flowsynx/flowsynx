﻿using FlowSync.Models;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace FlowSync.ApplicationBuilders;

public class CliApplicationBuilder : ICliApplicationBuilder
{
    private readonly ILogger<CliApplicationBuilder> _logger;
    private readonly RootCommand _rootCommand;

    public CliApplicationBuilder(ILogger<CliApplicationBuilder> logger, RootCommand rootCommand)
    {
        _logger = logger;
        _rootCommand = rootCommand;
    }

    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            var commandLineBuilder = new CommandLineBuilder(_rootCommand).UseDefaults();
            return await commandLineBuilder.Build().InvokeAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return ExitCode.Error;
        }
    }
}