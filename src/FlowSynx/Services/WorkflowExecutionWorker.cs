using FlowSynx.Application.Workflow;

namespace FlowSynx.Services;

public class WorkflowExecutionWorker : BackgroundService
{
    private readonly ILogger<WorkflowExecutionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowExecutionWorker(
        ILogger<WorkflowExecutionWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var workflowExecutionQueue = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionQueue>();
        var workflowOrchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();

        await foreach (var request in workflowExecutionQueue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Dequeued workflow {WorkflowId} execution {ExecutionId}", request.WorkflowId, request.ExecutionId);

                var workflow = await workflowOrchestrator.ExecuteWorkflowAsync(
                    request.UserId,
                    request.WorkflowId,
                    request.ExecutionId,
                    request.CancellationToken);

                await workflowExecutionQueue.CompleteAsync(request.ExecutionId, stoppingToken);
                _logger.LogInformation("Workflow {WorkflowId} execution {ExecutionId} finished with status {Status}",
                    request.WorkflowId, request.ExecutionId, workflow);
            }
            catch (Exception ex)
            {
                await workflowExecutionQueue.FailAsync(request.ExecutionId, stoppingToken);
                _logger.LogError(ex, "Error while executing workflow {WorkflowId} execution {ExecutionId}",
                    request.WorkflowId, request.ExecutionId);
            }
        }
    }
}