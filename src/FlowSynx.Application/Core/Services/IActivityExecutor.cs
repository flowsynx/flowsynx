using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.ActivityInstances;

namespace FlowSynx.Application.Core.Services;

public interface IActivityExecutor
{
    Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    bool CanExecute(ExecutableComponent executable);
}