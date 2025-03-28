using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.Azure.Blobs.Extensions;

internal static class ConverterExtensions
{
    public static async Task<PluginContextData> ToContextData(this BlobClient blobClient, bool? includeMetadata,
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

        var entity = new PluginContextData(blobClient.Name, "File")
        {
            RawData = rawData,
            Content = content,
        };

        if (includeMetadata is true)
        {
            var blobProperties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            AddProperties(entity, blobProperties);
        }

        return entity;
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

    private static void AddProperties(PluginContextData entity, BlobProperties properties)
    {
        entity.Metadata.Add("AccessTier", properties.AccessTier);
        entity.Metadata.Add("AccessTierChangedOn", properties.AccessTierChangedOn);
        entity.Metadata.Add("AccessTierInferred", properties.AccessTierInferred);
        entity.Metadata.Add("BlobSequenceNumber", properties.BlobSequenceNumber);
        entity.Metadata.Add("BlobType", properties.BlobType);
        entity.Metadata.Add("CacheControl", properties.CacheControl);
        entity.Metadata.Add("ContentDisposition", properties.ContentDisposition);
        entity.Metadata.Add("ContentEncoding", properties.ContentEncoding);
        entity.Metadata.Add("ContentHash", properties.ContentHash.ToHexString());
        entity.Metadata.Add("ContentLanguage", properties.ContentLanguage);
        entity.Metadata.Add("ContentLength", properties.ContentLength);
        entity.Metadata.Add("ContentType", properties.ContentType);
        entity.Metadata.Add("CopyCompletedOn", properties.CopyCompletedOn);
        entity.Metadata.Add("CopyId", properties.CopyId);
        entity.Metadata.Add("CopyProgress", properties.CopyProgress);
        entity.Metadata.Add("CopySource", properties.CopySource);
        entity.Metadata.Add("CopyStatus", properties.CopyStatus);
        entity.Metadata.Add("CopyStatusDescription", properties.CopyStatusDescription);
        entity.Metadata.Add("CreatedOn", properties.CreatedOn);
        entity.Metadata.Add("DestinationSnapshot", properties.DestinationSnapshot);
        entity.Metadata.Add("ETag", properties.ETag);
        entity.Metadata.Add("LastModified", properties.LastModified);
        entity.Metadata.Add("LeaseDuration", properties.LeaseDuration);
        entity.Metadata.Add("LeaseState", properties.LeaseState);
        entity.Metadata.Add("LeaseStatus", properties.LeaseStatus);
    }
}