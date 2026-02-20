using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using FlowSynx.Infrastructure.Runtime.Execution;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Executors;

public class ContainerActivityExecutor : BaseActivityExecutor
{
    public ContainerActivityExecutor(ILogger<ContainerActivityExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "container";
    }

    public override async Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var container = activity.Specification.Executable.Container;
        if (container == null)
        {
            throw new Exception("Container configuration is missing");
        }

        var image = container.Image;
        if (string.IsNullOrEmpty(image))
        {
            throw new Exception("Container image is not specified");
        }

        _logger.LogInformation("Starting container: {Image}", image);

        // In a real implementation, use Docker.DotNet or Kubernetes client
        // For now, we'll mock it
        await Task.Delay(500); // Simulate container startup

        var result = new
        {
            container = new
            {
                image = image,
                command = container.Command,
                args = container.Args
            },
            parameters = parameters,
            executedAt = DateTime.UtcNow,
            success = true,
            mock = true // Indicate this is a mock execution
        };

        return result;
    }
}