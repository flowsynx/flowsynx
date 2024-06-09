using System.CommandLine;
using System.Diagnostics;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Logging;
using FlowSynx.Services;

namespace FlowSynx.Commands;

public class Root : RootCommand
{
    public Root(ILogger<Root> logger, IOptionsVerifier optionsVerifier, IApiApplicationBuilder apiApplicationBuilder)
        : base("A system for managing and synchronizing data between different repositories and storage, including cloud, local, and etc.")
    {
        var configFileOption = new Option<string>(new[] { "--config-file" }, description: "FlowSynx configuration file");

        var enableHealthCheckOption = new Option<bool>(new[] { "--enable-health-check" }, getDefaultValue: () => true,
            description: "Enable health checks for the FlowSynx");

        var enableLogOption = new Option<bool>(new[] { "--enable-log" }, getDefaultValue: () => true,
            description: "Enable logging to records the details of events during FlowSynx running");

        var logLevelOption = new Option<LoggingLevel>(new[] { "--log-level" }, getDefaultValue: () => LoggingLevel.Info,
            description: "The log verbosity to controls the amount of detail emitted for each event that is logged");

        var logFileOption = new Option<string?>(new[] { "--log-file" },
            description: "Log file path to store system logs information");

        var openApiOption = new Option<bool>(new[] { "--open-api" }, getDefaultValue: () => false,
            description: "Enable OpenApi specification for FlowSynx");

        AddOption(configFileOption);
        AddOption(enableHealthCheckOption);
        AddOption(enableLogOption);
        AddOption(logLevelOption);
        AddOption(logFileOption);
        AddOption(openApiOption);

        this.SetHandler(async (options) =>
        {
            try
            {
                if (IsAnotherInstanceRunning())
                {
                    logger.LogError("Another instance(s) of the FlowSynx system is already running.");
                    return;
                }

                optionsVerifier.Verify(ref options);
                await apiApplicationBuilder.RunAsync(options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        },
        new RootOptionsBinder(configFileOption, enableHealthCheckOption, enableLogOption, 
            logLevelOption, logFileOption, openApiOption));
    }
    
    private bool IsAnotherInstanceRunning()
    {
        var currentProcess = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(currentProcess.ProcessName, ".");
        return processes.Any(process => process.Id != currentProcess.Id);
    }
}