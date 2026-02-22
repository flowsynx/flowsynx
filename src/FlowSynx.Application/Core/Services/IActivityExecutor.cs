using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;

namespace FlowSynx.Application.Core.Services;

public interface IActivityExecutor
{
    Task<object?> ExecuteAsync(
        ActivityJson activity,
        Domain.Workflows.ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    bool CanExecute(ExecutableComponent executable);
}