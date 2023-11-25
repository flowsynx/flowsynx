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
        var portOption = new Option<int>(new[] { "-p", "--port" }, getDefaultValue: () => 4400, description: "The port FlowSync is listening on");
        var configOption = new Option<string>(new[] { "-c", "--config-file" }, description: "FlowSync configuration file");
        var enableHealthCheckOption = new Option<bool>(new[] { "-H", "--enable-health-check" }, getDefaultValue: () => true, description: "Enable health checks for the FlowSync");
        var enableLogOption = new Option<bool>(new[] { "-L", "--enable-log" }, getDefaultValue: () => true, description: "Enable logging to records the details of events during FlowSync running");
        var logLevelOption = new Option<AppLogLevel>(new[] { "-l", "--log-level" }, getDefaultValue: () => AppLogLevel.Information, description: "The log verbosity to controls the amount of detail emitted for each event that is logged");
        var retryOption = new Option<int>(new[] { "-r", "--retry" }, getDefaultValue: () => 3, description: "The number of times FlowSync needs to try to receive data if there is a connection problem");

        this.AddOption(portOption);
        this.AddOption(configOption);
        this.AddOption(enableHealthCheckOption);
        this.AddOption(enableLogOption);
        this.AddOption(logLevelOption);
        this.AddOption(retryOption);

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
        new RootOptionsBinder(portOption, configOption, enableHealthCheckOption, 
            enableLogOption, logLevelOption, retryOption));
    }
}