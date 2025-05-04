using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Workflow;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Services;

public class TriggerProcessingService : BackgroundService
{
    private readonly ILogger<TriggerProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TriggerProcessingService(ILogger<TriggerProcessingService> logger, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var timeTriggerProcessor = scope.ServiceProvider.GetRequiredService<WorkflowTimeBasedTriggerProcessor>();
                //var eventTriggerProcessor = scope.ServiceProvider.GetRequiredService<EventBasedTriggerProcessor>();
                //var apiTriggerProcessor = scope.ServiceProvider.GetRequiredService<ApiBasedTriggerProcessor>();

                await Task.WhenAll(
                    timeTriggerProcessor.ProcessTriggersAsync(cancellationToken)
                    //eventTriggerProcessor.ListenForEventsForUserAsync(),
                    //apiTriggerProcessor.CheckForApiCallsForUserAsync()
                );
            }
            catch (Exception ex)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationTriggerProcessing, ex.Message);
                _logger.LogError(errorMessage.ToString());
            }

            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }
}