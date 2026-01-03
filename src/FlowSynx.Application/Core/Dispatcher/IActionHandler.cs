namespace FlowSynx.Application.Core.Dispatcher;

public interface IActionHandler<TAction, TResult>
    where TAction : IAction<TResult>
{
    Task<TResult> Handle(
        TAction action, 
        CancellationToken cancellationToken = default);
}