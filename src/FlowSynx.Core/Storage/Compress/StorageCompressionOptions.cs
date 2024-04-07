using FlowSynx.IO.Compression;

namespace FlowSynx.Core.Storage.Compress;

public class StorageCompressionOptions
{
    public CompressType CompressType { get; set; } = CompressType.Zip;
}