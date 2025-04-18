using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow;

public interface IRetryService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, RetryPolicy policy);
}