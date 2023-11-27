using EnsureThat;
using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Models;
using FlowSync.Core.Exceptions;
using FlowSync.Core.FileSystem.Parers.RemotePath;
using FlowSync.Core.Plugins;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.FileSystem;

internal class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;
    private readonly IPluginsManager _pluginsManager;
    private readonly IRemotePathParser _remotePathParser;

    public FileSystemService(ILogger<FileSystemService> logger, IPluginsManager pluginsManager, IRemotePathParser remotePathParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        EnsureArg.IsNotNull(remotePathParser, nameof(remotePathParser));
        _logger = logger;
        _pluginsManager = pluginsManager;
        _remotePathParser = remotePathParser;
    }

    public async Task<Usage> About(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var pathParser = _remotePathParser.Parse(path);
            var plugin = _pluginsManager.GetPlugin(pathParser.FileSystemType);
            plugin.SetSpecifications(pathParser.Specifications);
            return await plugin.About(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem getting about information. Message: {ex.Message}");
            throw new FileSystemException(ex.Message);
        }
    }

    public async Task<IEnumerable<FileSystemEntity>> List(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default)
    {
        try
        {
            var pathParser = _remotePathParser.Parse(path);
            var plugin = _pluginsManager.GetPlugin(pathParser.FileSystemType);
            plugin.SetSpecifications(pathParser.Specifications);
            return await plugin.ListAsync(pathParser.Path, fileSystemFilters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem getting entities list. Message: {ex.Message}");
            throw new FileSystemException(ex.Message);
        }
    }

    public async Task Delete(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default)
    {
        try
        {
            var pathParser = _remotePathParser.Parse(path);
            var plugin = _pluginsManager.GetPlugin(pathParser.FileSystemType);
            plugin.SetSpecifications(pathParser.Specifications);
            await plugin.DeleteAsync(pathParser.Path, fileSystemFilters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem getting entities list. Message: {ex.Message}");
            throw new FileSystemException(ex.Message);
        }
    }

    public async Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var pathParser = _remotePathParser.Parse(path);
            var plugin = _pluginsManager.GetPlugin(pathParser.FileSystemType);
            plugin.SetSpecifications(pathParser.Specifications);
            return await plugin.ReadAsync(pathParser.Path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"FileSystem read path. Message: {ex.Message}");
            throw new FileSystemException(ex.Message);
        }
    }
}