using FlowSynx.PluginCore;
using FlowSynx.Plugins.LocalFileSystem;
using System.Text;

namespace FlowSynx.Plugins.Storage.LocalFileSystem.Extensions;

internal static class ConverterExtensions
{
    public static PluginContextData ToContextData(this FileInfo file, bool? includeMetadata)
    {
        var dataBytes = File.ReadAllBytes(file.FullName);
        var isBinaryFile = IsBinaryFile(dataBytes);
        var rawData = isBinaryFile ? dataBytes : null;
        var content = !isBinaryFile ? Encoding.UTF8.GetString(dataBytes) : null;

        var entity = new PluginContextData(PathHelper.ToUnixPath(file.FullName), "File")
        {
            Format = file.Extension.ToLower(),
            RawData = rawData,
            Content = content,
        };

        if (includeMetadata is true)
        {
            entity.TryAddMetadata("Length", file.Length);
            entity.TryAddMetadata("CreatedTime", file.CreationTimeUtc);
            entity.TryAddMetadata("ModifiedTime", file.LastWriteTimeUtc);
            entity.TryAddMetadata("Attributes", file.Attributes.ToString());
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
}