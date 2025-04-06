using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.Azure.Blobs.Extensions;

internal static class ConverterExtensions
{
    public static async Task<PluginContext> ToContext(this BlobClient blobClient, bool? includeMetadata,
        CancellationToken cancellationToken)
    {
        BlobDownloadInfo download = await blobClient.DownloadAsync(cancellationToken);

        var ms = new MemoryStream();
        await download.Content.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);

        var dataBytes = ms.ToArray();
        var isBinaryFile = IsBinaryFile(dataBytes);
        var rawData = isBinaryFile ? dataBytes : null;
        var content = !isBinaryFile ? Encoding.UTF8.GetString(dataBytes) : null;

        var context = new PluginContext(blobClient.Name, "File")
        {
            RawData = rawData,
            Content = content,
        };

        if (includeMetadata is true)
        {
            var blobProperties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            AddProperties(context, blobProperties);
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

    private static void AddProperties(PluginContext context, BlobProperties properties)
    {
        context.Metadata.Add("AccessTier", properties.AccessTier);
        context.Metadata.Add("AccessTierChangedOn", properties.AccessTierChangedOn);
        context.Metadata.Add("AccessTierInferred", properties.AccessTierInferred);
        context.Metadata.Add("BlobSequenceNumber", properties.BlobSequenceNumber);
        context.Metadata.Add("BlobType", properties.BlobType);
        context.Metadata.Add("CacheControl", properties.CacheControl);
        context.Metadata.Add("ContentDisposition", properties.ContentDisposition);
        context.Metadata.Add("ContentEncoding", properties.ContentEncoding);
        context.Metadata.Add("ContentHash", properties.ContentHash.ToHexString());
        context.Metadata.Add("ContentLanguage", properties.ContentLanguage);
        context.Metadata.Add("ContentLength", properties.ContentLength);
        context.Metadata.Add("ContentType", properties.ContentType);
        context.Metadata.Add("CopyCompletedOn", properties.CopyCompletedOn);
        context.Metadata.Add("CopyId", properties.CopyId);
        context.Metadata.Add("CopyProgress", properties.CopyProgress);
        context.Metadata.Add("CopySource", properties.CopySource);
        context.Metadata.Add("CopyStatus", properties.CopyStatus);
        context.Metadata.Add("CopyStatusDescription", properties.CopyStatusDescription);
        context.Metadata.Add("CreatedOn", properties.CreatedOn);
        context.Metadata.Add("DestinationSnapshot", properties.DestinationSnapshot);
        context.Metadata.Add("ETag", properties.ETag);
        context.Metadata.Add("LastModified", properties.LastModified);
        context.Metadata.Add("LeaseDuration", properties.LeaseDuration);
        context.Metadata.Add("LeaseState", properties.LeaseState);
        context.Metadata.Add("LeaseStatus", properties.LeaseStatus);
    }
}