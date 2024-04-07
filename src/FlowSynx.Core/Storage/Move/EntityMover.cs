using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.IO;
using FlowSynx.Plugin.Storage;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Storage.Move;

public class EntityMover : IEntityMover
{
    private readonly ILogger<EntityMover> _logger;

    public EntityMover(ILogger<EntityMover> logger)
    {
        _logger = logger;
    }

    public async Task Move(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, StorageSearchOptions searchOptions, StorageMoveOptions moveOptions, CancellationToken cancellationToken = default)
    {
        if (string.Equals(sourceStorageNormsInfo.Plugin.Name, destinationStorageNormsInfo.Plugin.Name, StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(sourceStorageNormsInfo.Path, destinationStorageNormsInfo.Path, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new StorageException(Resources.MoveTheSourceAndDestinationPathAreIdenticalAndOverlap);
        }

        bool isFile;
        if ((isFile = PathHelper.IsFile(sourceStorageNormsInfo.Path)) != PathHelper.IsFile(destinationStorageNormsInfo.Path))
            throw new StorageException(Resources.MoveDestinationPathIsDifferentThanSourcePath);

        if (isFile)
            MoveFile(sourceStorageNormsInfo.Plugin, sourceStorageNormsInfo.Path, destinationStorageNormsInfo.Plugin, destinationStorageNormsInfo.Path, cancellationToken);
        else
            MoveDirectory(sourceStorageNormsInfo.Plugin, sourceStorageNormsInfo.Path, destinationStorageNormsInfo.Plugin, destinationStorageNormsInfo.Path, searchOptions, cancellationToken);

        if (!PathHelper.IsRootPath(sourceStorageNormsInfo.Path))
            await sourceStorageNormsInfo.Plugin.DeleteAsync(sourceStorageNormsInfo.Path, searchOptions, cancellationToken);
    }

    private async void MoveFile(IStoragePlugin sourcePlugin, string sourceFile, IStoragePlugin destinationPlugin, string destinationFile, CancellationToken cancellationToken = default)
    {
        var sourceStream = await sourcePlugin.ReadAsync(sourceFile, new StorageHashOptions(), cancellationToken);
        await destinationPlugin.WriteAsync(destinationFile, sourceStream.Stream, new StorageWriteOptions() { Overwrite = true }, cancellationToken);
        _logger.LogInformation($"Move operation - From '{sourcePlugin.Name}' to '{destinationPlugin.Name}' for file '{sourceFile}'");
    }

    private async void MoveDirectory(IStoragePlugin sourcePlugin, string sourceDirectory, IStoragePlugin destinationPlugin, string destinationDirectory, StorageSearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        if (!PathHelper.IsRootPath(destinationDirectory))
            await destinationPlugin.MakeDirectoryAsync(destinationDirectory, cancellationToken);

        var entities = await sourcePlugin.ListAsync(sourceDirectory, searchOptions,
            new StorageListOptions(), new StorageHashOptions(), cancellationToken);

        var storageEntities = entities.ToList();
        foreach (string dirPath in storageEntities.Where(x => x.Kind == StorageEntityItemKind.Directory))
        {
            var destinationDir = dirPath.Replace(sourceDirectory, destinationDirectory);
            await destinationPlugin.MakeDirectoryAsync(destinationDir, cancellationToken);
            _logger.LogInformation($"Copy operation - From '{sourcePlugin.Name}' to '{destinationPlugin.Name}' for directory '{dirPath}'");
        }

        foreach (string file in storageEntities.Where(x => x.Kind == StorageEntityItemKind.File))
        {
            var destinationFile = file.Replace(sourceDirectory, destinationDirectory);
            MoveFile(sourcePlugin, file, destinationPlugin, destinationFile, cancellationToken);
        }
    }
}