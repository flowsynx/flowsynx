using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.Azure.Files.Extensions;

internal static class ConverterExtensions
{
    public static async Task<PluginContextData> ToContextData(this ShareFileClient fileClient, bool? includeMetadata,
        CancellationToken cancellationToken)
    {
        var stream = await fileClient.OpenReadAsync(cancellationToken: cancellationToken);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);

        var dataBytes = ms.ToArray();
        var isBinaryFile = IsBinaryFile(dataBytes);
        var rawData = isBinaryFile ? dataBytes : null;
        var content = !isBinaryFile ? Encoding.UTF8.GetString(dataBytes) : null;

        var entity = new PluginContextData(fileClient.Name, "File")
        {
            RawData = rawData,
            Content = content,
        };

        var filePropertiesResponse = await fileClient.GetPropertiesAsync(cancellationToken);
        ShareFileProperties properties = filePropertiesResponse.Value;

        if (includeMetadata is true)
        {
            AddProperties(entity, properties);
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

    private static void AddProperties(PluginContextData entity, ShareFileProperties properties)
    {
        entity.TryAddMetadata("Content-Type", properties.ContentType);
        entity.TryAddMetadata("Content-Length", properties.ContentLength.ToString());
        entity.TryAddMetadata("ETag", properties.ETag.ToString());
        entity.TryAddMetadata("LastModified", properties.LastModified.ToString("o"));
        entity.TryAddMetadata("Content-Encoding", properties.ContentEncoding?.ToString() ?? "N/A");
        entity.TryAddMetadata("Content-Disposition", properties.ContentDisposition ?? "N/A");
        entity.TryAddMetadata("Cache-Control", properties.CacheControl ?? "N/A");
        entity.TryAddMetadata("Content-MD5", properties.ContentHash != null ? properties.ContentHash.ToHexString() : "N/A");
        entity.TryAddMetadata("LeaseState", properties.LeaseState.ToString());
        entity.TryAddMetadata("LeaseStatus", properties.LeaseStatus.ToString());
        entity.TryAddMetadata("LeaseDuration", properties.LeaseDuration.ToString() ?? "N/A");
    }
}