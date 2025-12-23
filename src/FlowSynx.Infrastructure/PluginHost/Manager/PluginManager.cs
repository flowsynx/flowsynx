using FlowSynx.Application.Configuration.Integrations.PluginRegistry;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Domain;
using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.PluginHost.Cache;
using FlowSynx.Infrastructure.PluginHost.Loader;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Linq;

namespace FlowSynx.Infrastructure.PluginHost.Manager;

public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly PluginRegistryConfiguration _pluginRegistryConfiguration;
    private readonly IPluginsLocation _pluginsLocation;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginDownloader _pluginDownloader;
    private readonly IPluginCacheService _pluginCacheService;
    private readonly ILocalization _localization;
    private readonly IVersion _version;
    private const string PluginSearchPattern = "*.dll";

    public PluginManager(
        ILogger<PluginManager> logger,
        PluginRegistryConfiguration pluginRegistryConfiguration,
        IPluginsLocation pluginsLocation,
        ICurrentUserService currentUserService,
        IPluginService pluginService,
        IPluginDownloader pluginDownloader,
        IPluginCacheService pluginCacheService,
        ILocalization localization,
        IVersion version)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pluginRegistryConfiguration = pluginRegistryConfiguration ?? throw new ArgumentNullException(nameof(pluginRegistryConfiguration));
        _pluginsLocation = pluginsLocation ?? throw new ArgumentNullException(nameof(pluginsLocation));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
        _pluginDownloader = pluginDownloader ?? throw new ArgumentNullException(nameof(pluginDownloader));
        _pluginCacheService = pluginCacheService ?? throw new ArgumentNullException(nameof(pluginCacheService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public async Task<(IReadOnlyCollection<PluginFullDetailsInfo> Items, int TotalCount)> GetPluginsFullDetailsListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _currentUserService.ValidateAuthentication();

        var registries = _pluginRegistryConfiguration.Urls?.Count > 0
            ? _pluginRegistryConfiguration.Urls!
            : new List<string> {};

        var aggregated = new List<PluginFullDetailsInfo>();

        foreach (var registry in registries)
        {
            try
            {
                var items = await _pluginDownloader.GetPluginsFullDetailsListAsync(registry, cancellationToken);
                aggregated.AddRange(items.Select(i => new PluginFullDetailsInfo
                {
                    Type = i.Type,
                    CategoryTitle = i.CategoryTitle,
                    Description = i.Description,
                    Versions = i.Versions,
                    LatestVersion = i.LatestVersion,
                    Registry = registry
                }));
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to list plugins from registry {Registry}: {Message}", registry, ex.Message);
            }
        }

        // Distinct by Type + Registry to avoid duplicates across multiple calls to same registry URL
        var distinct = aggregated
            .GroupBy<PluginFullDetailsInfo, (string Type, string Registry)>(
                x => (x.Type, x.Registry),
                new PluginIdRegistryEqualityComparer())
            .Select(g => g.First())
            .ToList();

        var totalCount = distinct.Count;

        // Pagination
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        var skip = (page - 1) * pageSize;

        var paged = distinct
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return (paged, totalCount);
    }

    public async Task InstallAsync(string pluginType, string? currentVersion, CancellationToken cancellationToken)
    {
        var registries = _pluginRegistryConfiguration.Urls?.Count > 0
            ? _pluginRegistryConfiguration.Urls!
            : new List<string> {};

        var (_, effectiveVersion) = await ResolveRegistryAndVersionAsync(registries, pluginType, currentVersion, cancellationToken);

        if (await PluginAlreadyExists(pluginType, effectiveVersion, cancellationToken))
            return;

        var pluginMetadata = await GetFromRegistriesAsync(
            registries,
            async (url) => await _pluginDownloader.GetPluginMetadataAsync(url, pluginType, effectiveVersion, cancellationToken),
            onFailureLog: ex => _logger.LogDebug("Failed to fetch metadata for {PluginType}@{PluginVersion}: {Message}", pluginType, effectiveVersion, ex.Message));

        var pluginPackData = await GetFromRegistriesAsync(
            registries,
            async (url) => await _pluginDownloader.GetPluginDataAsync(url, pluginType, effectiveVersion, cancellationToken),
            onFailureLog: ex => _logger.LogDebug("Failed to fetch package for {PluginType}@{PluginVersion}: {Message}", pluginType, effectiveVersion, ex.Message));

        var pluginBytes = ExtractPluginFile(pluginPackData);
        ValidatePluginChecksum(pluginBytes, pluginMetadata.Checksum);

        var isPluginCompatible = IsPluginCompatible(pluginMetadata, _version.Version, out var warningMessage);
        if (!isPluginCompatible)
            throw new FlowSynxException((int)ErrorCode.PluginCompatibility, warningMessage);

        if (!string.IsNullOrEmpty(warningMessage))
            _logger.LogWarning(warningMessage);

        string pluginDirectory = GetPluginLocalDirectory(pluginType, effectiveVersion);
        await _pluginDownloader.ExtractPluginAsync(pluginDirectory, pluginBytes, cancellationToken);

        int installedCount = await InstallPluginAssemblies(pluginDirectory, pluginMetadata, cancellationToken);

        if (installedCount == 0)
            throw new FlowSynxException((int)ErrorCode.PluginInstallationNotFound,
                _localization.Get("Plugin_Install_NoPluginInstalled"));
    }

    private byte[] ExtractPluginFile(byte[] pluginData)
    {
        using var memoryStream = new MemoryStream(pluginData);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);

        var pluginEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".plugin", StringComparison.OrdinalIgnoreCase));
        if (pluginEntry == null)
            throw new FlowSynxException((int)ErrorCode.PluginNotFound, "No .plugin file found in the package.");

        using var entryStream = pluginEntry.Open();
        using var pluginStream = new MemoryStream();
        entryStream.CopyTo(pluginStream);
        return pluginStream.ToArray();
    }

    public async Task UpdateAsync(string pluginType, string currentVersion, string? targetVersion, CancellationToken cancellationToken)
    {
        await Uninstall(pluginType, currentVersion, cancellationToken);
        await InstallAsync(pluginType, targetVersion, cancellationToken);
    }

    public async Task Uninstall(string pluginType, string currentVersion, CancellationToken cancellationToken)
    {
        var pluginEntity = await _pluginService.Get(
            _currentUserService.UserId(),
            pluginType,
            currentVersion,
            cancellationToken);

        if (pluginEntity is null)
        {
            var errorMessage = new ErrorMessage(
                (int)ErrorCode.PluginNotFound,
                _localization.Get("PluginManager_PluginCouldNotFound", pluginType, currentVersion));

            throw new FlowSynxException(errorMessage);
        }

        try
        {
            // Remove plugin index from cache (should also release AssemblyLoadContext)
            var index = new PluginCacheIndex(
                _currentUserService.UserId(),
                pluginEntity.Type,
                pluginEntity.Version);

            _pluginCacheService.RemoveByIndex(index);

            var parentLocation = Directory.GetParent(pluginEntity.PluginLocation);
            if (parentLocation is { Exists: true })
            {
                RemoveReadOnlyAttribute(parentLocation);
                parentLocation.Attributes = FileAttributes.Normal;

                const int maxAttempts = 5;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        parentLocation.Delete(true);
                        break;
                    }
                    catch (IOException) when (attempt < maxAttempts)
                    {
                        // Handles file locks caused by handles not yet released
                        await Task.Delay(500, cancellationToken);
                    }
                    catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                }
            }

            await _pluginService.Delete(pluginEntity, cancellationToken);
            _logger.LogInformation(
                _localization.Get("PluginManager_PluginUninstalledSuccessfully", pluginType, currentVersion));
        }
        catch (Exception ex)
        {
            throw new FlowSynxException((int)ErrorCode.PluginUninstall, ex.Message);
        }
    }

    #region internal methods
    private async Task<bool> PluginAlreadyExists(string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        var exists = await _pluginService.IsExist(_currentUserService.UserId(), pluginType, pluginVersion, cancellationToken);
        if (!exists)
            return false;

        var errorMessage = new ErrorMessage(
            (int)ErrorCode.PluginCheckExistence,
            _localization.Get("PluginManager_Install_PluginIsAlreadyInstalled", pluginType, pluginVersion)
        );

        throw new FlowSynxException(errorMessage);
    }

    private void ValidatePluginChecksum(byte[] pluginData, string? checksum)
    {
        if (_pluginDownloader.ValidateChecksum(pluginData, checksum))
            return;

        throw new FlowSynxException((int)ErrorCode.PluginChecksumValidationFailed,
            _localization.Get("PluginManager_Install_ChecksumValidationFailed"));
    }

    private bool IsPluginCompatible(PluginInstallMetadata pluginMetadata, Version hostVersion, out string warning)
    {
        warning = string.Empty;

        if (!Version.TryParse(pluginMetadata.MinimumFlowSynxVersion, out var minVersion))
        {
            warning = _localization.Get("Plugin_Invalid_MinimumFlowSynxVersion", pluginMetadata.MinimumFlowSynxVersion);
            return false;
        }

        if (hostVersion < minVersion)
        {
            warning = _localization.Get("Plugin_HostVersion_IsOld", hostVersion, pluginMetadata.MinimumFlowSynxVersion);
            return false;
        }

        if (!string.IsNullOrEmpty(pluginMetadata.TargetFlowSynxVersion))
        {
            if (!Version.TryParse(pluginMetadata.TargetFlowSynxVersion, out var targetVersion))
            {
                warning = _localization.Get("Plugin_Invalid_TargetFlowSynxVersion", pluginMetadata.TargetFlowSynxVersion);
                return false;
            }

            if (hostVersion > targetVersion)
            {
                warning = _localization.Get("Plugin_HostVersion_IsNewer", pluginMetadata.Type, pluginMetadata.TargetFlowSynxVersion, hostVersion);
                return true;
            }
        }

        return true; // Fully compatible
    }

    private string GetPluginLocalDirectory(string pluginType, string pluginVersion)
    {
        var rootLocation = Path.Combine(_pluginsLocation.Path, _currentUserService.UserId());
        return Path.Combine(rootLocation, pluginType, pluginVersion);
    }

    private async Task<int> InstallPluginAssemblies(
        string pluginDirectory,
        PluginInstallMetadata metadata,
        CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var dllPath in Directory.GetFiles(pluginDirectory, PluginSearchPattern))
        {
            try
            {
                using var loader = new PluginLoader(dllPath);
                var result = await loader.LoadAsync(cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    _logger.LogDebug(_localization.Get("PluginManager_Install_ErrorLoading", result.ErrorMessage!));
                    loader.Dispose();
                    continue;
                }

                var plugin = result.PluginInstance;

                var pluginEntity = CreatePluginEntity(metadata, dllPath, plugin!);
                await _pluginService.Add(pluginEntity, cancellationToken);
                _logger.LogInformation(_localization.Get("PluginManager_Install_PluginInstalledSuccessfully",
                    metadata.Type, metadata.Version));

                count++;
            }
            catch (FlowSynxException ex)
            {
                _logger.LogDebug(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(_localization.Get("PluginManager_Install_ErrorLoading", ex.Message));
            }
        }

        return count;
    }

    private PluginEntity CreatePluginEntity(
        PluginInstallMetadata metadata,
        string dllPath,
        IPlugin plugin)
    {
        return new PluginEntity
        {
            Id = Guid.NewGuid(),
            Type = metadata.Type,
            Version = metadata.Version,
            Checksum = metadata.Checksum,
            PluginLocation = dllPath,
            UserId = _currentUserService.UserId(),
            Owners = metadata.Owners.ToList(),
            Description = metadata.Description,
            Copyright = metadata.Copyright,
            License = metadata.License,
            LicenseUrl = metadata.LicenseUrl,
            ProjectUrl = metadata.ProjectUrl,
            RepositoryUrl = metadata.RepositoryUrl,
            LastUpdated = metadata.LastUpdated,
            Specifications = CreateSpecifications(metadata.Specifications),
            Operations = CreateOperations(metadata.Operations)
        };

        static List<PluginSpecification> CreateSpecifications(IEnumerable<SpecificationMetadata> specs) =>
            specs.Select(x => new PluginSpecification
            {
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                DefaultValue = x.DefaultValue,
                IsRequired = x.IsRequired
            }).ToList();

        static List<PluginOperation> CreateOperations(IEnumerable<PluginOperationMetadata> ops) =>
            ops.Select(op => new PluginOperation
            {
                Name = op.Name,
                Description = op.Description,
                Parameters = op.Parameters.Select(p => new PluginOperationParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue,
                    IsRequired = p.IsRequired
                }).ToList()
            }).ToList();
    }

    private void RemoveReadOnlyAttribute(DirectoryInfo directoryInfo)
    {
        foreach (var fileInfo in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            if (fileInfo.IsReadOnly)
                fileInfo.IsReadOnly = false;
        }
    }

    private async Task<(string RegistryUrl, string Version)> ResolveRegistryAndVersionAsync(
        IEnumerable<string> registries,
        string pluginType,
        string? requestedVersion,
        CancellationToken cancellationToken)
    {
        // If version provided and not "latest", we don't need to query registries for versions.
        if (!string.IsNullOrWhiteSpace(requestedVersion) &&
            !requestedVersion.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            var chosenRegistry = registries.FirstOrDefault() ?? string.Empty;
            return (chosenRegistry, requestedVersion);
        }

        List<Exception> errors = new();

        foreach (var registry in registries)
        {
            try
            {
                var resolved = await ResolveVersionAsync(registry, pluginType, requestedVersion, cancellationToken);
                return (registry, resolved);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
                _logger.LogDebug("Version resolution failed on registry {Registry} for {PluginType}: {Message}",
                    registry, pluginType, ex.Message);
            }
        }

        var errorMsg = $"No available versions found for plugin '{pluginType}' across configured registries.";
        throw new FlowSynxException((int)ErrorCode.PluginNotFound, errorMsg);
    }

    private static async Task<T> GetFromRegistriesAsync<T>(
        IEnumerable<string> registries,
        Func<string, Task<T>> factory,
        Action<Exception>? onFailureLog = null)
    {
        List<Exception> errors = new();

        foreach (var registry in registries)
        {
            try
            {
                return await factory(registry).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
                onFailureLog?.Invoke(ex);
            }
        }

        throw new FlowSynxException((int)ErrorCode.PluginNotFound, "Could not fetch data from any plugin registry.");
    }

    private async Task<string> ResolveVersionAsync(string registryUrl, string pluginType, string? requestedVersion, CancellationToken cancellationToken)
    {
        // If version is provided and not "latest", use it as-is
        if (!string.IsNullOrWhiteSpace(requestedVersion) &&
            !requestedVersion.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            return requestedVersion;
        }

        // Fetch versions from a specific registry and pick the latest
        var versions = await _pluginDownloader.GetPluginVersionsAsync(registryUrl, pluginType, cancellationToken);

        // Try IsLatest flag first
        var latest = versions?.FirstOrDefault(v => v.IsLatest == true)?.Version;

        // Fallback: try to pick max by System.Version when possible
        if (string.IsNullOrWhiteSpace(latest))
        {
            latest = versions?
                .Where(v => !string.IsNullOrWhiteSpace(v.Version))
                .Select(v => new
                {
                    raw = v.Version!,
                    parsed = Version.TryParse(v.Version, out var ver) ? ver : null
                })
                .OrderByDescending(x => x.parsed is not null)
                .ThenByDescending(x => x.parsed) // parsed versions first by numeric comparison
                .ThenByDescending(x => x.raw, StringComparer.OrdinalIgnoreCase) // fallback to string compare
                .FirstOrDefault()
                ?.raw;
        }

        if (string.IsNullOrWhiteSpace(latest))
            throw new FlowSynxException((int)ErrorCode.PluginNotFound, $"No available versions found for plugin '{pluginType}'.");

        _logger.LogInformation("Resolved latest version for plugin {PluginType} from {Registry}: {Version}", pluginType, registryUrl, latest);
        return latest!;
    }

    private sealed class PluginIdRegistryEqualityComparer : IEqualityComparer<(string Type, string Registry)>
    {
        public bool Equals((string Type, string Registry) x, (string Type, string Registry) y)
        {
            return string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Registry, y.Registry, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Type, string Registry) obj)
        {
            return (obj.Type?.ToLowerInvariant().GetHashCode() ?? 0) ^ (obj.Registry?.ToLowerInvariant().GetHashCode() ?? 0);
        }
    }
    #endregion
}
