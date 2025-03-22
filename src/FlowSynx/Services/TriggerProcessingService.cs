using FlowSynx.Infrastructure.Workflow;

namespace FlowSynx.Services;

public class TriggerProcessingService : BackgroundService
{
    private readonly ILogger<TriggerProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TriggerProcessingService(ILogger<TriggerProcessingService> logger, IServiceProvider serviceProvider)
    {
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
                _logger.LogError($"Error in Trigger Processing Service. Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}
