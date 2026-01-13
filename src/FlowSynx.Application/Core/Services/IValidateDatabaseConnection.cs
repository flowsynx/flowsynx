namespace FlowSynx.Application.Core.Services;

public interface IValidateDatabaseConnection
{
    Task<bool> ValidateConnection(CancellationToken cancellationToken = default);
}