using System.CommandLine;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Core.Services;
using FlowSynx.Environment;
using FlowSynx.Logging;
using FlowSynx.Services;

namespace FlowSynx.Commands;

public class Root : RootCommand
{
    public Root(ILogger<Root> logger, ILocation location, IEnvironmentManager environmentManager,
        IOptionsVerifier optionsVerifier, IApiApplicationBuilder apiApplicationBuilder)
        : base("Command Line Interface application for FlowSynx")
    {
        var configFileOption = new Option<string>(new[] { "--config-file" }, description: "FlowSynx configuration file");

        var enableHealthCheckOption = new Option<bool>(new[] { "--enable-health-check" }, getDefaultValue: () => true,
            description: "Enable health checks for the FlowSynx");

        var enableLogOption = new Option<bool>(new[] { "--enable-log" }, getDefaultValue: () => true,
            description: "Enable logging to records the details of events during FlowSynx running");

        var logLevelOption = new Option<LoggingLevel>(new[] { "--log-level" }, getDefaultValue: () => LoggingLevel.Info,
            description: "The log verbosity to controls the amount of detail emitted for each event that is logged");

        var logFileOption = new Option<string?>(new[] { "--log-file" },
            description: "The log verbosity to controls the amount of detail emitted for each event that is logged");

        AddOption(configFileOption);
        AddOption(enableHealthCheckOption);
        AddOption(enableLogOption);
        AddOption(logLevelOption);
        AddOption(logFileOption);

        this.SetHandler(async (options) =>
        {
            try
            {
                var flowSynxPath = environmentManager.Get(EnvironmentVariables.FlowsynxPath);
                if (string.IsNullOrEmpty(flowSynxPath))
                    environmentManager.Set(EnvironmentVariables.FlowsynxPath, location.RootLocation);

                var flowSynxPort = environmentManager.Get(EnvironmentVariables.FlowsynxHttpPort);
                if (string.IsNullOrEmpty(flowSynxPort))
                    environmentManager.Set(EnvironmentVariables.FlowsynxHttpPort, EnvironmentVariables.FlowsynxDefaultPort.ToString());

                optionsVerifier.Verify(ref options);
                await apiApplicationBuilder.RunAsync(options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        },
        new RootOptionsBinder(configFileOption, enableHealthCheckOption, enableLogOption, logLevelOption, logFileOption));
    }
}