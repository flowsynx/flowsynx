namespace FlowSynx.Application.Core.Dispatcher;

public interface IDispatcher
{
    Task<TResult> Dispatch<TResult>(IAction<TResult> request, CancellationToken cancellationToken = default);
}