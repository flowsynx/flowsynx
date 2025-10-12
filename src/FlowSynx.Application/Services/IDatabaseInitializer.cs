namespace FlowSynx.Application.Services;

public interface IDatabaseInitializer
{
    Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default);
}
