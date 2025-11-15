namespace FlowSynx.Infrastructure.PluginHost.Manager;

/// <summary>
/// Resolves the effective plugin version to install based on the registry state.
/// </summary>
public interface IPluginVersionResolver
{
    /// <summary>
    /// Resolves the version string that should be used for installation.
    /// </summary>
    /// <param name="registryUrl">The configured plugin registry base url.</param>
    /// <param name="pluginType">The plugin identifier.</param>
    /// <param name="requestedVersion">The requested version or <c>latest</c>.</param>
    /// <param name="cancellationToken">Cancellation token propagated from the request.</param>
    /// <returns>The resolved version string that exists in the registry.</returns>
    Task<string> ResolveVersionAsync(
        string registryUrl,
        string pluginType,
        string? requestedVersion,
        CancellationToken cancellationToken);
}
