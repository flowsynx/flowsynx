namespace FlowSynx.Application.Core.Dispatcher;

public interface Instruction<TResult> : IAction<TResult> { }

public interface ICommand : Instruction<Void> { }