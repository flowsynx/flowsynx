using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.Azure.Files.Extensions;

internal static class ConverterExtensions
{
    public static async Task<PluginContext> ToContext(this ShareFileClient fileClient, bool? includeMetadata,
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

        var context = new PluginContext(fileClient.Name, "File")
        {
            RawData = rawData,
            Content = content,
        };

        var filePropertiesResponse = await fileClient.GetPropertiesAsync(cancellationToken);
        ShareFileProperties properties = filePropertiesResponse.Value;

        if (includeMetadata is true)
        {
            AddProperties(context, properties);
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

    private static void AddProperties(PluginContext context, ShareFileProperties properties)
    {
        context.TryAddMetadata("Content-Type", properties.ContentType);
        context.TryAddMetadata("Content-Length", properties.ContentLength.ToString());
        context.TryAddMetadata("ETag", properties.ETag.ToString());
        context.TryAddMetadata("LastModified", properties.LastModified.ToString("o"));
        context.TryAddMetadata("Content-Encoding", properties.ContentEncoding?.ToString() ?? "N/A");
        context.TryAddMetadata("Content-Disposition", properties.ContentDisposition ?? "N/A");
        context.TryAddMetadata("Cache-Control", properties.CacheControl ?? "N/A");
        context.TryAddMetadata("Content-MD5", properties.ContentHash != null ? properties.ContentHash.ToHexString() : "N/A");
        context.TryAddMetadata("LeaseState", properties.LeaseState.ToString());
        context.TryAddMetadata("LeaseStatus", properties.LeaseStatus.ToString());
        context.TryAddMetadata("LeaseDuration", properties.LeaseDuration.ToString() ?? "N/A");
    }
}