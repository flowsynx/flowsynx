using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO;
using EnsureThat;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Security;
using FlowSynx.Connectors.Storage.LocalFileSystem.Extensions;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Services;

public class LocalFileManager : ILocalFileManager
{
    private readonly ILogger _logger;

    public LocalFileManager(ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public Task CreateAsync(string entity, CreateOptions options)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (options.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        return Task.CompletedTask;
    }

    public Task WriteAsync(string entity, WriteOptions options, object dataOptions)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && options.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        if (File.Exists(path) && options.Overwrite is true)
            DeleteAsync(path);

        using (var fileStream = File.Create(path))
        {
            dataStream.CopyTo(fileStream);
        }

        return Task.CompletedTask;
    }

    public Task<ReadResult> ReadAsync(string entity, ReadOptions options)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var file = new FileInfo(path);

        var result = new ReadResult
        {
            Content = File.ReadAllBytes(path),
            ContentHash = HashHelper.Md5.GetHash(file)
        };

        return Task.FromResult(result);
    }

    public Task DeleteAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (PathHelper.IsDirectory(path))
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning($"The specified path '{path}' is not exist.");
                return Task.CompletedTask;
            }

            DeleteAllEntities(path);
            Directory.Delete(path);
            _logger.LogInformation($"The specified path '{path}' was deleted successfully.");
        }
        else
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning($"The specified path '{path}' is not exist.");
                return Task.CompletedTask;
            }

            File.Delete(path);
            _logger.LogInformation($"The specified path '{path}' was deleted successfully.");
        }

        return Task.CompletedTask;
    }

    public Task PurgeAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        Directory.Delete(path, true);
        return Task.CompletedTask;
    }

    public Task<bool> ExistAsync(string entity)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrWhiteSpace(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        return Task.FromResult(PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path));
    }


    public Task<IEnumerable<StorageEntity>> ListAsync(string entity, ListOptions listOptions)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        if (!Directory.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var storageEntities = new List<StorageEntity>();
        var directoryInfo = new DirectoryInfo(path);

        storageEntities.AddRange(directoryInfo.FindFiles("*", listOptions.Recurse)
            .Select(file => file.ToEntity(listOptions.IncludeMetadata)));

        storageEntities.AddRange(directoryInfo.FindDirectories("*", listOptions.Recurse)
            .Select(dir => dir.ToEntity(listOptions.IncludeMetadata)));

        return Task.FromResult<IEnumerable<StorageEntity>>(storageEntities);
    }

    #region internal methods
    private void DeleteAllEntities(string path)
    {
        var di = new DirectoryInfo(path);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }
    }
    #endregion
}