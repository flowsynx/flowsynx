using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.PluginHost.Manager;

/// <summary>
/// Default implementation for determining the effective plugin version the manager should install.
/// </summary>
public class PluginVersionResolver : IPluginVersionResolver
{
    private readonly IPluginDownloader _pluginDownloader;
    private readonly ILogger<PluginVersionResolver> _logger;

    public PluginVersionResolver(
        IPluginDownloader pluginDownloader,
        ILogger<PluginVersionResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(pluginDownloader);
        ArgumentNullException.ThrowIfNull(logger);

        _pluginDownloader = pluginDownloader;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ResolveVersionAsync(
        string registryUrl,
        string pluginType,
        string? requestedVersion,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedVersion) &&
            !requestedVersion.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            return requestedVersion;
        }

        var versions = await _pluginDownloader.GetPluginVersionsAsync(registryUrl, pluginType, cancellationToken);
        var resolvedVersion = TrySelectVersionByIsLatest(versions) ??
                              TrySelectVersionByValue(versions);

        if (string.IsNullOrWhiteSpace(resolvedVersion))
        {
            throw new FlowSynxException(
                (int)ErrorCode.PluginNotFound,
                $"No available versions found for plugin '{pluginType}'.");
        }

        _logger.LogInformation("Resolved latest version for plugin {PluginType}: {Version}", pluginType, resolvedVersion);
        return resolvedVersion;
    }

    private static string? TrySelectVersionByIsLatest(IEnumerable<PluginVersion>? versions)
    {
        return versions?
            .FirstOrDefault(v => v.IsLatest == true)?
            .Version;
    }

    private static string? TrySelectVersionByValue(IEnumerable<PluginVersion>? versions)
    {
        return versions?
            .Where(v => !string.IsNullOrWhiteSpace(v.Version))
            .Select(v => new
            {
                Raw = v.Version!,
                Parsed = Version.TryParse(v.Version, out var parsed) ? parsed : null
            })
            .OrderByDescending(item => item.Parsed is not null)
            .ThenByDescending(item => item.Parsed)
            .ThenByDescending(item => item.Raw, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?.Raw;
    }
}
