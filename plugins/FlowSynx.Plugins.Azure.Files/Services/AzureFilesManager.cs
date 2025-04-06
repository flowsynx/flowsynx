using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using Azure;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Azure.Files.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Plugins.Azure.Files.Exceptions;
using System.Text;
using FlowSynx.Plugins.Azure.Files.Extensions;

namespace FlowSynx.Plugins.Azure.Files.Services;

public class AzureFilesManager: IAzureFilesManager
{
    private readonly ILogger _logger;
    private readonly ShareClient _client;

    public AzureFilesManager(ILogger logger, ShareClient client)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(client);
        _logger = logger;
        _client = client;
    }

    public async Task Create(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var createParameters = parameters.ToObject<CreateParameters>();
        await CreateEntity(createParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Delete(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var deleteParameters = parameters.ToObject<DeleteParameters>();
        await DeleteEntity(deleteParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Exist(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var existParameters = parameters.ToObject<ExistParameters>();
        return await ExistEntity(existParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PluginContext>> List(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var listParameters = parameters.ToObject<ListParameters>();
        return await ListEntities(listParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Purge(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var purgeParameters = parameters.ToObject<PurgeParameters>();
        await PurgeEntity(purgeParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var readParameters = parameters.ToObject<ReadParameters>();
        return await ReadEntity(readParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Write(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var writeParameters = parameters.ToObject<WriteParameters>();
        await WriteEntity(writeParameters, cancellationToken).ConfigureAwait(false);
    }

    #region internal methods
    private async Task CreateEntity(CreateParameters createParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(createParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesPathMustBeNotEmpty, Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesPathIsNotDirectory, Resources.ThePathIsNotDirectory);

        try
        {
            var pathParts = PathHelper.Split(path);
            string proceedPath = string.Empty;
            foreach (var part in pathParts)
            {
                proceedPath = PathHelper.Combine(proceedPath, part);
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(proceedPath);
                await directoryClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesResourceNotExist, string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesShareItemNotFound, string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesInvalidPathEntered, Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesParentNotFound, Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesSomethingWrongHappenedDuringProcessing, 
                "Something wrong happened during processing existing file on Azure file share!");
        }
    }

    private async Task WriteEntity(WriteParameters writeParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(writeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        var dataValue = writeParameters.Data;
        var pluginContextes = new List<PluginContext>();

        if (dataValue is PluginContext pluginContext)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            pluginContextes.Add(pluginContext);
        }
        else if (dataValue is IEnumerable<PluginContext> pluginContextesList)
        {
            if (!PathHelper.IsDirectory(path))
                throw new Exception(Resources.ThePathIsNotDirectory);

            pluginContextes.AddRange(pluginContextesList);
        }
        else if (dataValue is string data)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            var context = CreateContextFromStringData(path, data);
            pluginContextes.Add(context);
        }
        else
        {
            throw new NotSupportedException("The entered data format is not supported!");
        }

        foreach (var context in pluginContextes)
        {
            await WriteEntityFromContext(path, context, writeParameters.Overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    private PluginContext CreateContextFromStringData(string path, string data)
    {
        var rootPath = Path.GetPathRoot(path);
        string relativePath = path;

        if (!string.IsNullOrEmpty(rootPath))
            relativePath = Path.GetRelativePath(rootPath, path);

        var dataBytesArray = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        return new PluginContext(relativePath, "File")
        {
            RawData = dataBytesArray,
        };
    }

    private async Task WriteEntityFromContext(string path, PluginContext context, bool overwrite,
        CancellationToken cancellationToken)
    {
        byte[] dataToWrite;

        if (context.RawData is not null)
            dataToWrite = context.RawData;
        else if (context.Content is not null)
            dataToWrite = Encoding.UTF8.GetBytes(context.Content);
        else
            throw new InvalidDataException($"The entered data is invalid for '{context.Id}'");

        var rootPath = Path.GetPathRoot(context.Id);
        string relativePath = context.Id;

        if (!string.IsNullOrEmpty(rootPath))
            relativePath = Path.GetRelativePath(rootPath, context.Id);

        var fullPath = PathHelper.IsDirectory(path) ? PathHelper.Combine(path, relativePath) : path;

        if (!PathHelper.IsFile(fullPath))
            throw new Exception(Resources.ThePathIsNotFile);

        try
        {
            var fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (isExist && overwrite is false)
                throw new FlowSynxException((int)ErrorCodes.AzureFilesFileIsAlreadyExistAndCannotBeOverwritten, string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var parentPath = PathHelper.GetParent(path) + PathHelper.PathSeparatorString;
            var createParameters = new CreateParameters { Path = parentPath };
            await CreateEntity(createParameters, cancellationToken);

            using var stream = new MemoryStream(dataToWrite);
            await fileClient.CreateAsync(maxSize: stream.Length, cancellationToken: cancellationToken);
            await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesResourceNotExist, string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesShareItemNotFound, string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidResourceName)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesTheResourceNameContainsInvalidCharacters, Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesInvalidPathEntered, Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesParentNotFound, Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesSomethingWrongHappenedDuringProcessing,
                "Something wrong happened during processing existing file on Azure file share!");
        }
    }

    private async Task<PluginContext> ReadEntity(ReadParameters readParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesPathMustBeNotEmpty, Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesThePathMustBeFile, Resources.ThePathIsNotFile);

        try
        {
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (!isExist)
                throw new FlowSynxException((int)ErrorCodes.AzureFilesThePathIsNotExist, string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            return await fileClient.ToContext(true, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesResourceNotExist, string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesShareItemNotFound, string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesInvalidPathEntered, Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesParentNotFound, Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesSomethingWrongHappenedDuringProcessing,
                "Something wrong happened during processing existing file on Azure file share!");
        }
    }

    private async Task DeleteEntity(DeleteParameters deleteParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(deleteParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesPathMustBeNotEmpty, Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
                await fileClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await DeleteAll(directoryClient, cancellationToken: cancellationToken);
            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesResourceNotExist, string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesShareItemNotFound, string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesInvalidPathEntered, Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesParentNotFound, Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesSomethingWrongHappenedDuringProcessing,
                "Something wrong happened during processing existing file on Azure file share!");
        }
    }

    private async Task PurgeEntity(PurgeParameters purgeParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(purgeParameters.Path);
        ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
        await directoryClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task DeleteAll(ShareDirectoryClient dirClient, CancellationToken cancellationToken)
    {

        await foreach (ShareFileItem item in dirClient.GetFilesAndDirectoriesAsync())
        {
            if (item.IsDirectory)
            {
                var subDir = dirClient.GetSubdirectoryClient(item.Name);
                await DeleteAll(subDir, cancellationToken: cancellationToken);
            }
            else
            {
                await dirClient.DeleteFileAsync(item.Name, cancellationToken: cancellationToken);
            }
        }

        await dirClient.DeleteAsync(cancellationToken);
    }

    private async Task<bool> ExistEntity(ExistParameters existParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(existParameters.Path);
        if (string.IsNullOrWhiteSpace(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesThePathMustBeFile, Resources.ThePathMustBeFile);

        try
        {
            if (PathHelper.IsDirectory(path))
            {
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
                return await directoryClient.ExistsAsync(cancellationToken: cancellationToken);
            }

            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
            return await fileClient.ExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesResourceNotExist, string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesShareItemNotFound, string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesInvalidPathEntered, Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesParentNotFound, Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new FlowSynxException((int)ErrorCodes.AzureFilesSomethingWrongHappenedDuringProcessing,
                "Something wrong happened during processing existing file on Azure file share!");
        }
    }

    private async Task<IEnumerable<PluginContext>> ListEntities(ListParameters listParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(listParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesPathMustBeNotEmpty, Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new FlowSynxException((int)ErrorCodes.AzureFilesPathIsNotDirectory, Resources.ThePathIsNotDirectory);

        var storageEntities = new List<PluginContext>();
        ShareDirectoryClient directoryClient;

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
            directoryClient = _client.GetRootDirectoryClient();
        else
            directoryClient = _client.GetDirectoryClient(path);

        var remaining = new Queue<ShareDirectoryClient>();
        remaining.Enqueue(directoryClient);
        while (remaining.Count > 0)
        {
            ShareDirectoryClient dir = remaining.Dequeue();
            try
            {
                await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken))
                {
                    try
                    {
                        var fileClient = dir.GetFileClient(item.Name);
                        storageEntities.Add(await fileClient.ToContext(listParameters.IncludeMetadata, cancellationToken));

                        if (listParameters.Recurse is false) 
                            continue;

                        if (item.IsDirectory)
                        {
                            remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                        }
                    }
                    catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
                    {
                        _logger.LogError(string.Format(Resources.ShareItemNotFound, item.Name));
                    }
                }
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
            {
                throw new FlowSynxException((int)ErrorCodes.AzureFilesResourceNotExist, 
                    string.Format(Resources.ResourceNotExist, dir.Name));
            }
        }

        return storageEntities;
    }
    #endregion
}