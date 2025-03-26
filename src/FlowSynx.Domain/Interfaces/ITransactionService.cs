namespace FlowSynx.Domain.Interfaces;

public interface ITransactionService
{
    Task TransactionAsync(Func<Task> action, CancellationToken cancellationToken);
}