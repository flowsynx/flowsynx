using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Net;
using FlowSynx.IO;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Security;
using FlowSynx.Commons;
using FlowSynx.Plugin.Storage.Options;
using FlowSynx.IO.Compression;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

public class LocalFileSystemStorage : IPlugin
{
    private readonly ILogger<LocalFileSystemStorage> _logger;
    private readonly IStorageFilter _storageFilter;

    public LocalFileSystemStorage(ILogger<LocalFileSystemStorage> logger, IStorageFilter storageFilter)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _storageFilter = storageFilter;
    }

    public Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public string Name => "LocalFileSystem";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public Task Initialize()
    {
        return Task.CompletedTask;
    }
    
    public Task<object> About(PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var aboutOptions = options.ToObject<AboutOptions>();
        long totalSpace = 0, freeSpace = 0;
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d is not { DriveType: DriveType.Fixed, IsReady: true }) continue;

                totalSpace += d.TotalSize;
                freeSpace += d.TotalFreeSpace;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            freeSpace = 0;
        }

        var result = new
        {
            Total = totalSpace.ToString(!aboutOptions.Full),
            Free = freeSpace.ToString(!aboutOptions.Full),
            Used = (totalSpace - freeSpace).ToString(!aboutOptions.Full)
        };
        return Task.FromResult<object>(result);
    }

    public Task<object> CreateAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (createOptions.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        var result = new StorageEntity(path, StorageEntityItemKind.Directory);
        return Task.FromResult<object>(new { result.Id });
    }

    public Task<object> WriteAsync(string entity, PluginOptions? options, 
        object dataOptions, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && writeOptions.Overwrite is false)
            throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException("Entered data is not valid. The data should be in string or Base64 format.");

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();
        
        if (File.Exists(path) && writeOptions.Overwrite is true)
            DeleteEntityAsync(path);
        
        using var fileStream = File.Create(path);
        dataStream.CopyTo(fileStream);
        var result = new StorageEntity(path, StorageEntityItemKind.File);
        return Task.FromResult<object>(new { result.Id });
    }

    public Task<object> ReadAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readOptions = options.ToObject<ReadOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var file = new FileInfo(path);
        var fileExtension = file.Extension;
        var result = new StorageRead()
        {
            Stream = new StorageStream(File.OpenRead(path)),
            ContentType = fileExtension.GetContentType(),
            Extension = fileExtension,
            Md5 = HashHelper.Md5.GetHash(file)
        };

        return Task.FromResult<object>(result);
    }

    public Task<object> UpdateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<object>> DeleteAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var deleteOptions = options.ToObject<DeleteOptions>();
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var result = new List<string>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList list) 
                continue;

            if (DeleteEntityAsync(list.Path))
            {
                result.Add(list.Id);
            }
        }

        if (deleteOptions.Purge is true)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            Directory.Delete(path, true);
        }

        return result;
    }

    public Task<bool> ExistAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        return Task.FromResult<bool>(PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path));
    }

    public Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);
        
        if (!Directory.Exists(path))
            throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var listOptions = options.ToObject<ListOptions>();

        if (!string.IsNullOrEmpty(listOptions.Kind) && !EnumUtils.TryParseWithMemberName<StorageEntityItemKind>(listOptions.Kind, out _))
            throw new StorageException(Resources.ListValidatorKindValueMustBeValidMessage);
        
        var storageEntities = new List<StorageEntity>();
        var directoryInfo = new DirectoryInfo(path);

        storageEntities.AddRange(directoryInfo.FindFiles("*", listOptions.Recurse)
            .Select(file => file.ToEntity(listOptions.Hashing, listOptions.IncludeMetadata)));

        storageEntities.AddRange(directoryInfo.FindDirectories("*", listOptions.Recurse)
            .Select(dir => dir.ToEntity(listOptions.IncludeMetadata)));

        var filteredEntities = _storageFilter.Filter(storageEntities, options).ToList();

        var result = new List<StorageList>(filteredEntities.Count());
        result.AddRange(filteredEntities.Select(storageEntity => new StorageList
        {
            Id = storageEntity.Id,
            Kind = storageEntity.Kind.ToString().ToLower(),
            Name = storageEntity.Name,
            Path = storageEntity.FullPath,
            CreatedTime = storageEntity.CreatedTime,
            ModifiedTime = storageEntity.ModifiedTime,
            Size = storageEntity.Size.ToString(!listOptions.Full),
            ContentType = storageEntity.ContentType,
            Md5 = storageEntity.Md5,
            Metadata = storageEntity.Metadata
        }));

        return Task.FromResult<IEnumerable<object>>(result);
    }

    public async Task<IEnumerable<TransmissionData>> PrepareTransmissionData(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (PathHelper.IsFile(entity))
        {
            var copyFile = await PrepareCopyFile(entity, cancellationToken);
            return new List<TransmissionData>() { copyFile };
        }

        return await PrepareCopyDirectory(entity, options, cancellationToken);
    }

    private async Task<TransmissionData> PrepareCopyFile(string entity, CancellationToken cancellationToken = default)
    {
        var sourceStream = await ReadAsync(entity, null, cancellationToken);

        if (sourceStream is not StorageRead storageRead)
            throw new StorageException($"Copy operation for file '{entity} could not proceed!'");
        
        return new TransmissionData(entity, storageRead.Stream, storageRead.ContentType);
    }

    private async Task<IEnumerable<TransmissionData>> PrepareCopyDirectory(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var entities = await ListAsync(entity, options, cancellationToken).ConfigureAwait(false);
        var storageEntities = entities.ToList().ConvertAll(item => (StorageList)item);

        var result = new List<TransmissionData>(storageEntities.Count);

        foreach (var entityItem in storageEntities)
        {
            TransmissionData transmissionData;
            if (string.Equals(entityItem.Kind, "file", StringComparison.OrdinalIgnoreCase))
            {
                var read = await ReadAsync(entityItem.Path, null, cancellationToken);
                if (read is not StorageRead storageRead)
                {
                    _logger.LogWarning($"The item '{entityItem.Name}' could be not read.");
                    continue;
                }
                transmissionData = new TransmissionData(entityItem.Path, storageRead.Stream, storageRead.ContentType);
            }
            else
            {
                transmissionData = new TransmissionData(entityItem.Path);
            }

            result.Add(transmissionData);
        }

        return result;
    }

    public async Task<IEnumerable<object>> TransmitDataAsync(string entity, PluginOptions? options, IEnumerable<TransmissionData> transmissionData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = new List<object>();
        var data = transmissionData.ToList();
        foreach (var item in data)
        {
            switch (item.Content)
            {
                case null:
                    result.Add(await CreateAsync(item.Key, options, cancellationToken));
                    _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    break;
                case StorageStream stream:
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await CreateAsync(parentPath, options, cancellationToken);
                        result.Add(await WriteAsync(item.Key, options, stream, cancellationToken));
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                    break;
            }
        }

        return result;
    }
    
    public async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList entry)
            {
                _logger.LogWarning("The item is not valid object type. It should be StorageEntity type.");
                continue;
            }

            if (!string.Equals(entry.Kind, "file", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"The item '{entry.Name}' is not a file.");
                continue;
            }

            try
            {
                var stream = await ReadAsync(entry.Path, options, cancellationToken);
                if (stream is not StorageRead storageRead)
                {
                    _logger.LogWarning($"The item '{entry.Name}' could be not read.");
                    continue;
                }

                compressEntries.Add(new CompressEntry
                {
                    Name = entry.Name,
                    ContentType = entry.ContentType,
                    Stream = storageRead.Stream,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                continue;
            }
        }

        return compressEntries;
    }

    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    #region internal methods
    private bool DeleteEntityAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (PathHelper.IsDirectory(path))
        {
            if (!Directory.Exists(path)) 
                return false;

            DeleteAllEntities(path);
            Directory.Delete(path);
        }
        else
        {
            if (!File.Exists(path))
                return false;
            
            File.Delete(path);
        }
        return true;
    }

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

    public void Dispose() { }
}