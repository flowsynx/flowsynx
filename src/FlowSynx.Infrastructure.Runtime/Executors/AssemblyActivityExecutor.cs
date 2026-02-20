using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using FlowSynx.Infrastructure.Runtime.Execution;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Executors;

public class AssemblyActivityExecutor : BaseActivityExecutor
{
    public AssemblyActivityExecutor(ILogger<AssemblyActivityExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "assembly";
    }

    public override async Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var executable = activity.Specification.Executable;
        var assemblyPath = executable.Assembly;

        if (string.IsNullOrEmpty(assemblyPath))
        {
            throw new Exception("Assembly path is not specified");
        }

        _logger.LogInformation("Loading assembly: {AssemblyPath}", assemblyPath);

        try
        {
            // In a real implementation, use reflection to load and execute
            // For now, we'll mock it
            await Task.Delay(200); // Simulate loading

            var result = new
            {
                assembly = assemblyPath,
                entryPoint = executable.EntryPoint,
                parameters = parameters,
                context = new
                {
                    activity = instance.Id,
                    blueprint = activity.Metadata.Name
                },
                executedAt = DateTime.UtcNow,
                success = true
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute assembly {AssemblyPath}", assemblyPath);
            throw new Exception($"Assembly execution failed: {ex.Message}", ex);
        }
    }
}