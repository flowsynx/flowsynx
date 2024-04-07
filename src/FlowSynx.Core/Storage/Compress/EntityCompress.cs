using EnsureThat;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Storage;
using FlowSynx.Security;

namespace FlowSynx.Core.Storage.Compress;

public class EntityCompress : IEntityCompress
{
    private readonly Func<CompressType, ICompression> _compressionFactory;

    public EntityCompress(Func<CompressType, ICompression> compressionFactory)
    {
        EnsureArg.IsNotNull(compressionFactory, nameof(compressionFactory));
        _compressionFactory = compressionFactory;
    }

    public async Task<CompressResult> Compress(StorageNormsInfo storageNormsInfo, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageCompressionOptions compressionOptions,
        CancellationToken cancellationToken = default)
    {
        var entities = await storageNormsInfo.Plugin.ListAsync(storageNormsInfo.Path, searchOptions,
                listOptions, new StorageHashOptions(), cancellationToken);

        var storageEntities = entities.Where(x => x.IsFile).ToList();
        if (storageEntities == null || !storageEntities.Any())
        {
            throw new StorageException("No file found to make a compression archive.");
        }

        var en = new List<CompressEntry>();
        foreach (var entity in storageEntities)
        {
            var stream = await storageNormsInfo.Plugin.ReadAsync(entity.FullPath, new StorageHashOptions(), cancellationToken);
            en.Add(new CompressEntry
            {
                Name = entity.Name,
                ContentType = entity.ContentType,
                Stream = stream.Stream
            });
        }

        var compressResult = await _compressionFactory(compressionOptions.CompressType).Compress(en);
        var md5Hash = string.Empty;

        if (hashOptions.Hashing is true)
        {
            md5Hash = HashHelper.GetMd5Hash(compressResult.Stream);
        }

        return new CompressResult { Stream = compressResult.Stream, ContentType = compressResult.ContentType, Md5 = md5Hash };
    }
}