﻿using FlowSynx.PluginCore;
using FlowSynx.Plugins.LocalFileSystem.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Extensions;
using FlowSynx.Plugins.Storage.LocalFileSystem.Extensions;
using System.Text;

namespace FlowSynx.Plugins.LocalFileSystem.Services;

internal class LocalFileManager : ILocalFileManager
{
    private readonly IPluginLogger _logger;
    public LocalFileManager(IPluginLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task Create(PluginParameters parameters)
    {
        var createParameters = parameters.ToObject<CreateParameters>();
        await CreateEntity(createParameters).ConfigureAwait(false);
    }

    public async Task Delete(PluginParameters parameters)
    {
        var deleteParameter = parameters.ToObject<DeleteParameters>();
        await DeleteEntity(deleteParameter).ConfigureAwait(false);
    }

    public async Task<bool> Exist(PluginParameters parameters)
    {
        var existParameters = parameters.ToObject<ExistParameters>();
        return await ExistEntity(existParameters).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PluginContext>> List(PluginParameters parameters)
    {
        var listParameter = parameters.ToObject<ListParameters>();
        return await ListEntities(listParameter).ConfigureAwait(false);
    }

    public async Task Purge(PluginParameters parameters)
    {
        var purgeParameters = parameters.ToObject<PurgeParameters>();
        await PurgeEntity(purgeParameters).ConfigureAwait(false);
    }

    public async Task<PluginContext> Read(PluginParameters parameters)
    {
        var readParameters = parameters.ToObject<ReadParameters>();
        return await ReadEntity(readParameters).ConfigureAwait(false);
    }

    public async Task Rename(PluginParameters parameters)
    {
        var renameParameters = parameters.ToObject<RenameParameters>();
        await RenameEntity(renameParameters).ConfigureAwait(false);
    }

    public async Task Write(PluginParameters parameters)
    {
        var writeParameters = parameters.ToObject<WriteParameters>();
        await WriteEntity(writeParameters).ConfigureAwait(false);
    }

    #region internal methods
    private Task CreateEntity(CreateParameters createParameters)
    {
        var path = PathHelper.ToUnixPath(createParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        var directory = Directory.CreateDirectory(path);
        if (createParameters.Hidden is true)
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

        return Task.CompletedTask;
    }

    private Task DeleteEntity(DeleteParameters deleteParameters)
    {
        var path = PathHelper.ToUnixPath(deleteParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotFile, path));

        if (!File.Exists(path))
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        File.Delete(path);
        _logger.LogInfo($"The specified path '{path}' was deleted successfully.");

        return Task.CompletedTask;
    }

    private Task<bool> ExistEntity(ExistParameters existParameters)
    {
        var path = PathHelper.ToUnixPath(existParameters.Path);
        if (string.IsNullOrWhiteSpace(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        var isExist = PathHelper.IsDirectory(path) ? Directory.Exists(path) : File.Exists(path);
        return Task.FromResult(isExist);
    }

    private Task<IEnumerable<PluginContext>> ListEntities(ListParameters listParameters)
    {
        var path = PathHelper.ToUnixPath(listParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        if (!Directory.Exists(path))
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var directoryInfo = new DirectoryInfo(path);

        var fileEntities = directoryInfo
                           .FindFiles(_logger, listParameters)
                           .Select(file => file.ToContext(listParameters.IncludeMetadata))
                           .ToList();

        return Task.FromResult<IEnumerable<PluginContext>>(fileEntities);
    }

    private Task PurgeEntity(PurgeParameters purgeParameters)
    {
        var path = PathHelper.ToUnixPath(purgeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(string.Format(Resources.TheSpecifiedDirectoryPathIsNotDirectory, path));

        if (!Directory.Exists(path))
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var directoryInfo = new DirectoryInfo(path);
        var deleteRecursively = purgeParameters.Force ?? false;

        directoryInfo.Delete(deleteRecursively);
        _logger.LogInfo($"The specified path '{path}' was deleted successfully.");

        return Task.CompletedTask;
    }

    private Task<PluginContext> ReadEntity(ReadParameters readParameters)
    {
        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        if (!File.Exists(path))
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var fileInfo = new FileInfo(path);
        var entity = fileInfo.ToContext(true);

        return Task.FromResult(entity);
    }

    private Task RenameEntity(RenameParameters renameParameters)
    {
        var sourcePath = PathHelper.ToUnixPath(renameParameters.Path);
        var targetPath = PathHelper.ToUnixPath(renameParameters.TargetPath);

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            throw new Exception($"Error: Source '{sourcePath}' does not exist.");

        if (File.Exists(targetPath) || Directory.Exists(renameParameters.TargetPath))
            throw new Exception($"Error: Target '{renameParameters.TargetPath}' already exists.");

        if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, targetPath);
            _logger.LogInfo($"File renamed: '{sourcePath}' → '{targetPath}'");
        }
        else if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, targetPath);
            _logger.LogInfo($"Directory renamed: '{sourcePath}' → '{targetPath}'");
        }

        return Task.CompletedTask;
    }

    private async Task WriteEntity(WriteParameters writeParameters)
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
        else if (dataValue is IEnumerable<PluginContext> pluginContextDataList)
        {
            if (!PathHelper.IsDirectory(path))
                throw new Exception(Resources.ThePathIsNotDirectory);

            pluginContextes.AddRange(pluginContextDataList);
        }
        else if (dataValue is string data)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            var contextData = CreateContextDataFromStringData(path, data);
            pluginContextes.Add(contextData);
        }
        else
        {
            throw new NotSupportedException("The entered data format is not supported!");
        }

        foreach (var contextData in pluginContextes)
        {
            await WriteEntityFromContextData(path, contextData, writeParameters.Overwrite).ConfigureAwait(false);
        }
    }

    private PluginContext CreateContextDataFromStringData(string path, string data)
    {
        var root = Path.GetPathRoot(path);
        var relativePath = Path.GetRelativePath(root, path);
        var dataBytesArray = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        return new PluginContext(relativePath, "File")
        {
            RawData = dataBytesArray,
        };
    }

    private Task WriteEntityFromContextData(string path, PluginContext pluginContext, bool overwrite)
    {
        byte[] dataToWrite;

        if (pluginContext.RawData is not null)
            dataToWrite = pluginContext.RawData;
        else if (pluginContext.Content is not null)
            dataToWrite = Encoding.UTF8.GetBytes(pluginContext.Content);
        else
            throw new InvalidDataException($"The entered data is invalid for '{pluginContext.Id}'");

        var rootPath = Path.GetPathRoot(pluginContext.Id);
        string relativePath = pluginContext.Id;

        if (!string.IsNullOrEmpty(rootPath))
            relativePath = Path.GetRelativePath(rootPath, pluginContext.Id);

        var fullPath = PathHelper.IsDirectory(path) ? Path.Combine(path, relativePath) : path;

        var parentDirectory = Path.GetDirectoryName(fullPath);
        if (parentDirectory != null)
            Directory.CreateDirectory(parentDirectory);

        if (!PathHelper.IsFile(fullPath))
            throw new Exception(Resources.ThePathIsNotFile);

        if (File.Exists(fullPath) && overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, fullPath));

        if (File.Exists(fullPath))
        {
            if (overwrite is false)
                throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, fullPath));
            else
                DeleteEntity(new DeleteParameters { Path = fullPath });
        }

        File.WriteAllBytes(fullPath, dataToWrite);
        return Task.CompletedTask;
    }
    #endregion
}