using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Workflow;

namespace FlowSynx.Services;

public class TriggerProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _userId; // The user ID for which we are processing triggers

    public TriggerProcessingService(IServiceProvider serviceProvider, int userId)
    {
        _serviceProvider = serviceProvider;
        _userId = userId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var timeTriggerProcessor = scope.ServiceProvider.GetRequiredService<WorkflowTimeBasedTriggerProcessor>();
            //var eventTriggerProcessor = scope.ServiceProvider.GetRequiredService<EventBasedTriggerProcessor>();
            //var apiTriggerProcessor = scope.ServiceProvider.GetRequiredService<ApiBasedTriggerProcessor>();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Process triggers for the specific user
                await Task.WhenAll(
                    timeTriggerProcessor.ProcessTriggersAsync(stoppingToken)
                    //eventTriggerProcessor.ListenForEventsForUserAsync(_userId),
                    //apiTriggerProcessor.CheckForApiCallsForUserAsync(_userId)
                );

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
