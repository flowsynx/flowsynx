namespace FlowSynx.Infrastructure.Persistence;

public interface IDatabaseInitializer
{
    Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default);
}
