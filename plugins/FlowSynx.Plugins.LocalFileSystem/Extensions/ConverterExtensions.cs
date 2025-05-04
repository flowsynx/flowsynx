using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.LocalFileSystem.Extensions;

internal static class ConverterExtensions
{
    public static PluginContext ToContext(this FileInfo file, bool? includeMetadata)
    {
        var dataBytes = File.ReadAllBytes(file.FullName);
        var isBinaryFile = IsBinaryFile(dataBytes);
        var rawData = isBinaryFile ? dataBytes : null;
        var content = !isBinaryFile ? Encoding.UTF8.GetString(dataBytes) : null;

        var context = new PluginContext(PathHelper.ToUnixPath(file.FullName), "File")
        {
            Format = file.Extension.ToLower(),
            RawData = rawData,
            Content = content,
        };

        if (includeMetadata is true)
        {
            context.TryAddMetadata("Length", file.Length);
            context.TryAddMetadata("CreatedTime", file.CreationTimeUtc);
            context.TryAddMetadata("ModifiedTime", file.LastWriteTimeUtc);
            context.TryAddMetadata("Attributes", file.Attributes.ToString());
        }
        return context;
    }

    private static bool IsBinaryFile(byte[]? data, int sampleSize = 1024)
    {
        if (data == null || data.Length == 0)
            return false;

        var checkLength = Math.Min(sampleSize, data.Length);
        var nonPrintableCount = data.Take(checkLength)
            .Count(b => (b < 8 || (b > 13 && b < 32)) && b != 9 && b != 10 && b != 13);

        var threshold = 0.1; // 10% threshold of non-printable characters
        return (double)nonPrintableCount / checkLength > threshold;
    }
}