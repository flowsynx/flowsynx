using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System.Data;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Azure.Blobs.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Azure.Blobs.Extensions;
using System.Text;

namespace FlowSynx.Plugins.Azure.Blobs.Services;

public class AzureBlobManager : IAzureBlobManager
{
    private readonly ILogger _logger;
    private readonly BlobServiceClient _client;
    private readonly string _containerName;

    public AzureBlobManager(ILogger logger, BlobServiceClient client, string containerName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(client);
        _logger = logger;
        _client = client;
        _containerName = containerName;
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
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        try
        {
            var container = await GetBlobContainerClient().ConfigureAwait(false);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(path))
                _logger.LogWarning($"The Azure Blob storage doesn't support create empty directory.");
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new Exception(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new Exception(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new Exception(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new Exception(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new Exception(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
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

        var container = await GetBlobContainerClient().ConfigureAwait(false);
        BlockBlobClient blockBlobClient = container.GetBlockBlobClient(fullPath);

        var isExist = await blockBlobClient.ExistsAsync(cancellationToken);

        if (isExist && overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        using var stream = new MemoryStream(dataToWrite);
        await blockBlobClient.UploadAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<PluginContext> ReadEntity(ReadParameters readParameters,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        try
        {
            var container = await GetBlobContainerClient().ConfigureAwait(false);
            var blobClient = container.GetBlobClient(path);

            var isExist = await blobClient.ExistsAsync(cancellationToken);
            if (!isExist)
                throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            return await blobClient.ToContext(true, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new Exception(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new Exception(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new Exception(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new Exception(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new Exception(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task DeleteEntity(DeleteParameters deleteParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(deleteParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var container = await GetBlobContainerClient().ConfigureAwait(false);

            if (PathHelper.IsFile(path))
            {
                BlockBlobClient blockBlobClient = container.GetBlockBlobClient(path);
                await blockBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            var blobItems = container.GetBlobsAsync(prefix: path);
            await foreach (BlobItem blobItem in blobItems)
            {
                BlobClient blobClient = container.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, blobItem.Name));
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new Exception(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new Exception(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new Exception(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new Exception(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new Exception(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task PurgeEntity(PurgeParameters purgeParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(purgeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path)) 
            throw new Exception(Resources.ThePathIsNotDirectory);

        if (PathHelper.IsRootPath(path))
            throw new Exception($"The entered path '{path}' must be not root path!");

        var container = await GetBlobContainerClient().ConfigureAwait(false);

        var isExist = await container.ExistsAsync(cancellationToken);
        if (!isExist)
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        BlockBlobClient blockBlobClient = container.GetBlockBlobClient(path);
        var reponse = await blockBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        if (reponse.Value is false)
            throw new Exception($"The entered path '{path}' could not be purged!");
    }

    private async Task<bool> ExistEntity(ExistParameters existParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(existParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var container = await GetBlobContainerClient().ConfigureAwait(false);

            if (PathHelper.IsFile(path))
            {
                BlockBlobClient blockBlobClient = container.GetBlockBlobClient(path);
                return await blockBlobClient.ExistsAsync(cancellationToken: cancellationToken);
            }

            var blobItems = container.GetBlobsByHierarchy(prefix: path);
            return blobItems.Select(x => x.IsPrefix).Any();
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidResourceName")
        {
            throw new Exception(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidUri")
        {
            throw new Exception(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState")
        {
            throw new Exception(Resources.OperationNotAllowedInCurrentState);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ParentNotFound")
        {
            throw new Exception(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new Exception(Resources.SomethingWrongHappenedDuringProcessingExistingBlob);
        }
    }

    private async Task<List<PluginContext>> ListEntities(ListParameters listParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(listParameters.Path);

        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        var container = await GetBlobContainerClient().ConfigureAwait(false);
        return await ListBlobs(container, listParameters, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<PluginContext>> ListBlobs(BlobContainerClient containerClient, 
        ListParameters listParameters, CancellationToken cancellationToken)
    {
        var result = new List<PluginContext>();

        var resultSegment = containerClient.GetBlobsAsync(
            prefix: FormatFolderPrefix(listParameters.Path),
            traits: BlobTraits.Metadata,
            states: BlobStates.None
        ).AsPages(default, listParameters.MaxResults).ConfigureAwait(false);

        await foreach (var page in resultSegment)
        {
            foreach (var blobItem in page.Values)
            {
                try
                {
                    if (!listParameters.Recurse is true && blobItem.Name.Contains("/"))
                        continue; // Skip subdirectories if non-recursive mode is selected

                    BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var contextData = await blobClient.ToContext(listParameters.IncludeMetadata, cancellationToken).ConfigureAwait(false);
                    result.Add(contextData);

                    if (result.Count >= listParameters.MaxResults)
                        return result; // Stop if max results are reached
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        return result;
    }

    private string? FormatFolderPrefix(string folderPath)
    {
        folderPath = PathHelper.Normalize(folderPath);

        if (PathHelper.IsRootPath(folderPath))
            return null;

        if (!folderPath.EndsWith(PathHelper.PathSeparator))
            folderPath += PathHelper.PathSeparator;

        return folderPath;
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var container = _client.GetBlobContainerClient(_containerName);

        try
        {
            await container.GetPropertiesAsync().ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            throw new Exception(ex.Message);
        }

        return container;
    }
    #endregion
}
