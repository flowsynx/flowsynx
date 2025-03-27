using FlowSynx.PluginCore;
using FlowSynx.Plugins.LocalFileSystem;

namespace FlowSynx.Plugins.Storage.LocalFileSystem.Extensions;

internal static class ConverterExtensions
{
    public static PluginContextData ToContextData(this FileInfo file, bool? includeMetadata)
    {
        var isBinaryFile = IsBinaryFile(file.FullName);
        var rawData = isBinaryFile ? File.ReadAllBytes(file.FullName) : null;
        var content = !isBinaryFile ? File.ReadAllText(file.FullName) : null;

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

    private static bool IsBinaryFile(this string filePath, int sampleSize = 1024)
    {
        byte[] buffer = new byte[sampleSize];
        int bytesRead;

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            bytesRead = fs.Read(buffer, 0, buffer.Length);
        }

        // Define printable ASCII range (9 = Tab, 10 = LF, 13 = CR)
        int nonPrintableThreshold = 5; // Allow a few non-printable characters
        int nonPrintableCount = buffer.Take(bytesRead).Count(b => (b < 9 || (b > 13 && b < 32)) && b != 127);

        return nonPrintableCount > nonPrintableThreshold;
    }
}