namespace FlowSynx.Infrastructure.Workflow;

public interface IRetryService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, int maxRetries, TimeSpan delay);
}