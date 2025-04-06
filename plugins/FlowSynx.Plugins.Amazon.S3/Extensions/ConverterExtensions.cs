using Amazon.S3.Model;
using Amazon.S3;
using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.Amazon.S3.Extensions;

internal static class ConverterExtensions
{
    private const string MetaDataHeaderPrefix = "x-amz-meta-";

    public static async Task<PluginContext> ToContext(this AmazonS3Client client, string bucketName, string key,
        bool? includeMetadata, CancellationToken cancellationToken)
    {
        var request = new GetObjectRequest { BucketName = bucketName, Key = key };
        var response = await client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);

        var dataBytes = ms.ToArray();
        var isBinaryFile = IsBinaryFile(dataBytes);
        var rawData = isBinaryFile ? dataBytes : null;
        var content = !isBinaryFile ? Encoding.UTF8.GetString(dataBytes) : null;

        var context = new PluginContext(response.Key, "File")
        {
            RawData = rawData,
            Content = content
        };

        if (includeMetadata is true)
        {
            context.TryAddMetadata("Length", response.ContentLength);
            context.TryAddMetadata("ModifiedTime", response.LastModified.ToUniversalTime());
            context.TryAddMetadata("ContentHash", response.ETag.Trim('\"'));
            context.TryAddMetadata("StorageClass", response.StorageClass);
            context.TryAddMetadata("ETag", response.ETag);
            AddProperties(client, response.BucketName, response.Key, context, cancellationToken);
        }

        return context;
    }

    private static bool IsBinaryFile(byte[] data, int sampleSize = 1024)
    {
        if (data == null || data.Length == 0)
            return false;

        int checkLength = Math.Min(sampleSize, data.Length);
        int nonPrintableCount = data.Take(checkLength)
            .Count(b => (b < 8 || (b > 13 && b < 32)) && b != 9 && b != 10 && b != 13);

        double threshold = 0.1; // 10% threshold of non-printable characters
        return (double)nonPrintableCount / checkLength > threshold;
    }

    private static async void AddProperties(AmazonS3Client client, string bucketName, string key,
        PluginContext entity, CancellationToken cancellationToken)
    {
        GetObjectMetadataResponse obj = await client.GetObjectMetadataAsync(bucketName, key, cancellationToken).ConfigureAwait(false);
        if (obj != null)
        {
            entity.Format = obj.Headers.ContentType;
            AddProperties(entity, obj.Metadata);
        }
    }

    private static void AddProperties(PluginContext context, MetadataCollection metadata)
    {
        foreach (string key in metadata.Keys)
        {
            string value = metadata[key];
            string putKey = key;
            if (putKey.StartsWith(MetaDataHeaderPrefix))
                putKey = putKey[MetaDataHeaderPrefix.Length..];

            context.TryAddMetadata(putKey,value);
        }
    }
}
