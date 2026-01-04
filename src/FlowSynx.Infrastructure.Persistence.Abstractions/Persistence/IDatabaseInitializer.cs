namespace FlowSynx.Infrastructure.Persistence.Abstractions;

public interface IDatabaseInitializer
{
    Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default);
}