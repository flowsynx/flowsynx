using System.CommandLine;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Core.Services;
using FlowSynx.Enums;
using FlowSynx.Environment;
using FlowSynx.Services;

namespace FlowSynx.Commands;

public class Root : RootCommand
{
    public Root(ILogger<Root> logger, ILocation location, IEnvironmentManager environmentManager,
        IOptionsVerifier optionsVerifier, IApiApplicationBuilder apiApplicationBuilder)
        : base("Command Line Interface application for Cloud Storage")
    {
        var configOption = new Option<string>(new[] { "-c", "--config-file" }, description: "FlowSync configuration file");

        var enableHealthCheckOption = new Option<bool>(new[] { "-H", "--enable-health-check" }, getDefaultValue: () => true, 
            description: "Enable health checks for the FlowSync");

        var enableLogOption = new Option<bool>(new[] { "-L", "--enable-log" }, getDefaultValue: () => true, 
            description: "Enable logging to records the details of events during FlowSync running");

        var logLevelOption = new Option<AppLogLevel>(new[] { "-l", "--log-level" }, getDefaultValue: () => AppLogLevel.Information, 
            description: "The log verbosity to controls the amount of detail emitted for each event that is logged");

        var retryOption = new Option<int>(new[] { "-r", "--retry" }, getDefaultValue: () => 3, 
            description: "The number of times FlowSync needs to try to receive data if there is a connection problem");

        AddOption(configOption);
        AddOption(enableHealthCheckOption);
        AddOption(enableLogOption);
        AddOption(logLevelOption);
        AddOption(retryOption);

        this.SetHandler(async (options) =>
        {
            try
            {
                var flowSyncPath = environmentManager.Get(EnvironmentVariables.FlowsynxPath);
                if (string.IsNullOrEmpty(flowSyncPath))
                    environmentManager.Set(EnvironmentVariables.FlowsynxPath, location.RootLocation);

                var flowSyncPort = environmentManager.Get(EnvironmentVariables.FlowsynxHttpPort);
                if (string.IsNullOrEmpty(flowSyncPort))
                    environmentManager.Set(EnvironmentVariables.FlowsynxHttpPort, EnvironmentVariables.FlowsynxDefaultPort.ToString());

                optionsVerifier.Verify(ref options);
                await apiApplicationBuilder.RunAsync(options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        },
        new RootOptionsBinder(configOption, enableHealthCheckOption, enableLogOption, logLevelOption, retryOption));
    }
}