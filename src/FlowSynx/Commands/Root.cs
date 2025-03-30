using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using FlowSynx.ApplicationBuilders;
using FlowSynx.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Services;

namespace FlowSynx.Commands;

public class Root : RootCommand
{
    public Root(ILogger<Root> logger, IApiApplicationBuilder apiApplicationBuilder,
        IEndpoint endpoint)
        : base("A system for managing and synchronizing data between different repositories and storage, including cloud, local, and etc.")
    {
        this.SetHandler(async (InvocationContext context) =>
        {
            try
            {
                if (IsAnotherInstanceRunning())
                {
                    logger.LogError("Another instance(s) of the FlowSynx system is already running.");
                    return;
                }

                var cancellationToken = context.GetCancellationToken();
                await apiApplicationBuilder.RunAsync(logger, endpoint.HttpPort(), cancellationToken);
            }
            catch (FlowSynxException ex)
            {
                logger.LogError(ex.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally 
            {
                // Since SetHandler executes synchronously, if the console closes immediately,
                // the output may not be visible. So, added await Task.Delay(500) here;
                await Task.Delay(500);
            }
        });
    }
    
    private bool IsAnotherInstanceRunning()
    {
        var currentProcess = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(currentProcess.ProcessName, ".");
        return processes.Any(process => process.Id != currentProcess.Id);
    }
}