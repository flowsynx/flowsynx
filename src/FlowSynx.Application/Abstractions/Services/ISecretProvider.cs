using FlowSynx.Domain.TenantSecrets;

namespace FlowSynx.Application.Abstractions.Services;

public interface ISecretProvider
{
    Task<string?> GetSecretAsync(SecretKey secretKey, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string?>> GetSecretsAsync(string? prefix = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}