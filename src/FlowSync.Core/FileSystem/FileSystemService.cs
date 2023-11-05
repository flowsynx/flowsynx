using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Core.Exceptions;
using FlowSync.Core.FileSystem.Parse.RemotePath;
using FlowSync.Core.Services;
using FlowSync.Core.Wrapper;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.FileSystem;

internal class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;
    private readonly IPluginsManager _pluginsManager;
    private readonly IRemotePathParser _remotePathParser;

    public FileSystemService(ILogger<FileSystemService> logger, IPluginsManager pluginsManager, IRemotePathParser remotePathParser)
    {
        _logger = logger;
        _pluginsManager = pluginsManager;
        _remotePathParser = remotePathParser;
    }

    public async Task<IResult<IEnumerable<Entity>>> List(string path, FilterOptions filters, CancellationToken cancellationToken)
    {
        try
        {
            var pathParser = _remotePathParser.Parse(path);
            var plugin = _pluginsManager.GetPlugin(pathParser.FileSystemType);
            var loadedFileSystem = plugin.NewInstance(pathParser.Specifications);

            var result = await loadedFileSystem.ListAsync(pathParser.Path, filters, cancellationToken);
            return await Result<IEnumerable<Entity>>.SuccessAsync(result);
        }
        catch (DeserializerException ex)
        {
            _logger.LogError(ex.Message);
            return await Result<ICollection<Entity>>.FailAsync(new List<string> { ex.Message });
        }
        catch (PluginLoadingException ex)
        {
            _logger.LogError(ex.Message);
            return await Result<ICollection<Entity>>.FailAsync(new List<string> { ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return await Result<ICollection<Entity>>.FailAsync(ex.Message);
        }
    }
}