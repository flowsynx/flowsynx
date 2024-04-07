using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Compress;

public interface IEntityCompress
{
    Task<CompressResult> Compress(StorageNormsInfo storageNormsInfo, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageCompressionOptions compressionOptions,
        CancellationToken cancellationToken = default);
}