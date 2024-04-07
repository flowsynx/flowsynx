using FlowSynx.IO.Compression;

namespace FlowSynx.Core.Storage.Options;

public class StorageCompressionOptions
{
    public CompressType CompressType { get; set; } = CompressType.Zip;
}