namespace FlowSynx.Application.Core.Dispatcher;

public interface IDispatcher
{
    Task<TAction> Dispatch<TAction>(
        IAction<TAction> action, 
        CancellationToken cancellationToken = default);
}