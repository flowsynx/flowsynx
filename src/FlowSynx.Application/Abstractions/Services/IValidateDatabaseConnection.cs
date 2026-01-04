namespace FlowSynx.Application.Abstractions.Services;

public interface IValidateDatabaseConnection
{
    Task<bool> ValidateConnection(CancellationToken cancellationToken = default);
}