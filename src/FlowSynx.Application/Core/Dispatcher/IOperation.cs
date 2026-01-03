namespace FlowSynx.Application.Core.Dispatcher;

public interface IOperation<TResult> : IAction<TResult> { }
public interface IOperation : IOperation<Void> { }