using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using Amazon.S3.Transfer;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Amazon.S3.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Amazon.S3.Extensions;
using System.Text;
using System.Text.RegularExpressions;

namespace FlowSynx.Plugins.Amazon.S3.Services;

public class AmazonS3Manager : IAmazonS3Manager
{
    private readonly IPluginLogger _logger;
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly TransferUtility _fileTransferUtility;

    public AmazonS3Manager(IPluginLogger logger, AmazonS3Client client, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(client);
        _logger = logger;
        _client = client;
        _bucketName = bucketName;
        _fileTransferUtility = CreateTransferUtility(_client);
    }

    public async Task Create(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var createParameters = parameters.ToObject<CreateParameters>();
        await CreateEntity(createParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Delete(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var deleteParameter = parameters.ToObject<DeleteParameters>();
        await DeleteEntity(deleteParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Exist(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var existParameters = parameters.ToObject<ExistParameters>();
        return await ExistEntity(existParameters.Path, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PluginContext>> List(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var listParameter = parameters.ToObject<ListParameters>();
        return await ListEntities(listParameter, cancellationToken).ConfigureAwait(false);
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

        var isExist = await BucketExists(_bucketName, cancellationToken);
        if (!isExist)
        {
            await _client.PutBucketAsync(_bucketName, cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInfo($"Bucket '{_bucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(path))
        {
            var isFolderExist = await ExistEntity(path, cancellationToken);
            if (!isFolderExist)
                await AddFolder(_bucketName, path, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var bucketsResponse = await _client.ListBucketsAsync(cancellationToken);
            return bucketsResponse.Buckets.Any(x => x.BucketName == bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var request = new PutObjectRequest()
        {
            BucketName = bucketName,
            StorageClass = S3StorageClass.Standard,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.None,
            Key = folderName,
            ContentBody = string.Empty
        };
        await _client.PutObjectAsync(request, cancellationToken);
        _logger.LogInfo($"Folder '{folderName}' was created successfully.");
    }

    private async Task DeleteEntity(DeleteParameters deleteParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(deleteParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var isExist = await ExistEntity(path, cancellationToken);
            if (!isExist)
                throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            await _client
                .DeleteObjectAsync(_bucketName, path, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInfo(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<bool> ExistEntity(string path, CancellationToken cancellationToken)
    {
        path = PathHelper.ToUnixPath(path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var request = new ListObjectsRequest { BucketName = _bucketName, Prefix = path };
            var response = await _client.ListObjectsAsync(request, cancellationToken).ConfigureAwait(false);
            return response is { S3Objects.Count: > 0 };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<IEnumerable<PluginContext>> ListEntities(ListParameters listParameters,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(listParameters.Path);

        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        if (PathHelper.IsRootPath(path))
            throw new Exception("The path could be not root path!");

        return await ListObjects(_bucketName, listParameters, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<PluginContext>> ListObjects(string bucketName, ListParameters listParameters,
        CancellationToken cancellationToken)
    {
        int count = 0;
        string continuationToken;
        var result = new List<PluginContext>();

        Regex? regex = null;
        if (!string.IsNullOrEmpty(listParameters.Filter))
        {
            var regexOptions = listParameters.CaseSensitive is true ? RegexOptions.IgnoreCase : RegexOptions.None;
            regex = new Regex(listParameters.Filter, regexOptions);
        }

        var request = new ListObjectsV2Request()
        {
            BucketName = bucketName,
            Prefix = FormatFolderPrefix(listParameters.Path),
            Delimiter = listParameters.Recurse is true ? null : PathHelper.PathSeparatorString
        };

        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
            continuationToken = response.NextContinuationToken;

            foreach (var obj in response.S3Objects)
            {
                if (obj.Key.EndsWith(PathHelper.PathSeparator))
                    continue;

                if (count >= listParameters.MaxResults)
                    break;

                var isMatched = regex != null && regex.IsMatch(obj.Key);
                if (listParameters.Filter == null || isMatched)
                {
                    var context = await _client.ToContext(obj.BucketName, obj.Key, listParameters.IncludeMetadata, cancellationToken);
                    result.Add(context);
                    count++;
                }
            }

            if (count >= listParameters.MaxResults) break;
        }
        while (!string.IsNullOrEmpty(continuationToken));

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

    private async Task PurgeEntity(PurgeParameters purgeParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(purgeParameters.Path);
        var folder = path;
        if (!folder.EndsWith(PathHelper.PathSeparator))
            folder += PathHelper.PathSeparator;

        await DeleteEntity(new DeleteParameters { Path = folder }, cancellationToken);
    }

    private async Task<PluginContext> ReadEntity(ReadParameters readParameters, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        try
        {
            var isExist = await ExistEntity(path, cancellationToken);
            if (!isExist)
                throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            return await _client.ToContext(_bucketName, path, true, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
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
        var root = Path.GetPathRoot(path);
        var relativePath = Path.GetRelativePath(root, path);
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

        var isExist = await ExistEntity(fullPath, cancellationToken);
        if (isExist && overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, fullPath));

        using var stream = new MemoryStream(dataToWrite);

        try
        {
            await _fileTransferUtility
                  .UploadAsync(stream, _bucketName, fullPath, cancellationToken)
                  .ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
    }
    
    private TransferUtility CreateTransferUtility(AmazonS3Client client)
    {
        return new TransferUtility(client, new TransferUtilityConfig());
    }
    #endregion
}