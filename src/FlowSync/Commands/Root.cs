using System.CommandLine;
using FlowSync.ApplicationBuilders;
using FlowSync.Enums;
using FlowSync.Models;
using FlowSync.Services;

namespace FlowSync.Commands;

public class Root : RootCommand
{
    public Root(ILogger<Root> logger, IOptionsVerifier optionsVerifier, IApiApplicationBuilder apiApplicationBuilder) 
        : base("Command Line Interface application for Cloud Storage")
    {
        var portOption = new Option<int>(name: "--port", getDefaultValue: () => 4400, description: "The port FlowSync is listening on");
        var configOption = new Option<string>(name: "--config-file", description: "FlowSync configuration file");
        var enableLogOption = new Option<bool>(name: "--enable-log", getDefaultValue: () => true, description: "Enable logging to records the details of events during application running");
        var logLevelOption = new Option<AppLogLevel>(name: "--log-level", getDefaultValue: () => AppLogLevel.Information, description: "The log verbosity to controls the amount of detail emitted for each event that is logged");

        this.AddOption(portOption);
        this.AddOption(enableLogOption);
        this.AddOption(logLevelOption);
        this.AddOption(configOption);

        this.SetHandler(async (options) =>
        {
            try
            {
                optionsVerifier.Verify(ref options);
                await apiApplicationBuilder.RunAsync(options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        },
        new OptionsBinder(portOption, configOption, enableLogOption, logLevelOption));
    }
}