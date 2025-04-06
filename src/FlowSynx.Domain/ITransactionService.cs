namespace FlowSynx.Domain;

public interface ITransactionService
{
    Task TransactionAsync(Func<Task> action, CancellationToken cancellationToken);
}