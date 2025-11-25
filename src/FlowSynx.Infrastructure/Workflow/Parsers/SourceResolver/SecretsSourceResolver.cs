using FlowSynx.Application.Secrets;
using FlowSynx.Domain;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.Workflow.Parsers.SourceResolver;

/// <summary>
/// Source resolver for retrieving secrets using ISecretProvider
/// </summary>
internal class SecretsSourceResolver : ISourceResolver
{
    private readonly ISecretProvider _secretProvider;
    private readonly bool _throwOnMissing;
    private Dictionary<string, string>? _secretsCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public SecretsSourceResolver(ISecretProvider secretProvider, bool throwOnMissing = true)
    {
        _secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
        _throwOnMissing = throwOnMissing;
    }

    public async Task<object?> Resolve(string key, CancellationToken cancellationToken = default)
    {
        // Load secrets into cache if not already loaded
        if (_secretsCache == null)
        {
            _cacheLock.Wait();
            try
            {
                if (_secretsCache == null)
                {
                    var secrets = await _secretProvider.GetSecretsAsync(cancellationToken).ConfigureAwait(false);

                    _secretsCache = secrets.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value,
                        StringComparer.OrdinalIgnoreCase);
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        // Lookup secret from cache
        if (_secretsCache.TryGetValue(key, out var value))
            return value;

        if (_throwOnMissing)
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"ExpressionParser: Secret '{key}' not found in provider '{_secretProvider.Name}'");

        return null;
    }
}