using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Contract for fetching configuration secrets from Infisical.
/// </summary>
public interface IInfisicalSecretClient
{
    /// <summary>
    /// Retrieves the secret collection with keys mapped to configuration paths.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token propagated to the underlying SDK.</param>
    /// <returns>A read-only collection of configuration key/value pairs.</returns>
    Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(CancellationToken cancellationToken = default);
}
