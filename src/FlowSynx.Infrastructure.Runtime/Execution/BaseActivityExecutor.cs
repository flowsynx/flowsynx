using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public abstract class BaseActivityExecutor : IActivityExecutor
{
    protected readonly ILogger _logger;

    protected BaseActivityExecutor(ILogger logger)
    {
        _logger = logger;
    }

    public abstract Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    public abstract bool CanExecute(ExecutableComponent executable);

    protected Dictionary<string, object> PrepareContext(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> externalContext)
    {
        var context = new Dictionary<string, object>
        {
            ["activity"] = new
            {
                id = instance.Id,
                name = activity.Metadata.Name,
                version = activity.Metadata.Version
            },
            ["params"] = parameters,
            ["context"] = externalContext,
            ["config"] = instance.Configuration,
            ["blueprint"] = new
            {
                metadata = activity.Metadata,
                spec = new
                {
                    description = activity.Specification.Description,
                    executionProfile = activity.Specification.ExecutionProfile
                }
            }
        };

        foreach (var kvp in externalContext)
        {
            context[kvp.Key] = kvp.Value;
        }

        return context;
    }
}