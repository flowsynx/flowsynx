using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Services;

public class TriggerProcessingService : BackgroundService
{
    private readonly ILogger<TriggerProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TriggerProcessingService(ILogger<TriggerProcessingService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TriggerProcessingService started.");

        using var initialScope = _serviceProvider.CreateScope();
        var processors = initialScope.ServiceProvider.GetServices<IWorkflowTriggerProcessor>().ToList();

        // Launch each processor in its own background task
        var processorTasks = processors.Select(p => RunProcessorAsync(p, cancellationToken)).ToList();

        await Task.WhenAll(processorTasks);
    }

    private async Task RunProcessorAsync(IWorkflowTriggerProcessor processor, CancellationToken cancellationToken)
    {
        var processorType = processor.Name;
        var interval = GetProcessorInterval(processor);

        _logger.LogInformation("{ProcessorType} started with interval {Interval}.", processorType, interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedProcessor = scope.ServiceProvider
                    .GetServices<IWorkflowTriggerProcessor>()
                    .FirstOrDefault(p => p.GetType() == processor.GetType());

                if (scopedProcessor == null)
                {
                    _logger.LogWarning("{ProcessorType} not found in scope.", processorType);
                    continue;
                }

                await scopedProcessor.ProcessTriggersAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationTriggerProcessing, ex.Message);
                _logger.LogError(ex, "{ProcessorType} error: {Message}", processorType, errorMessage);
            }

            await Task.Delay(interval, cancellationToken);
        }

        _logger.LogInformation("{ProcessorType} stopped.", processorType);
    }

    private static TimeSpan GetProcessorInterval(IWorkflowTriggerProcessor processor) => processor.Interval;
}