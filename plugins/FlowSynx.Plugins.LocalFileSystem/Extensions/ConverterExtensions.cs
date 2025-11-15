using FlowSynx.PluginCore;
using System.Text;

namespace FlowSynx.Plugins.LocalFileSystem.Extensions;

/// <summary>
/// Provides helper conversions between local files and FlowSynx plugin contexts.
/// </summary>
internal static class ConverterExtensions
{
    /// <summary>
    /// Builds a <see cref="PluginContext"/> representation for a given file, optionally enriching metadata.
    /// </summary>
    /// <param name="file">The source file to convert.</param>
    /// <param name="includeMetadata">When true, populates file system metadata in the context.</param>
    /// <returns>A populated plugin context that contains either UTF-8 content or raw bytes.</returns>
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

    /// <summary>
    /// Performs a lightweight heuristic to determine whether the provided data represents binary content.
    /// </summary>
    /// <param name="data">The data to inspect.</param>
    /// <param name="sampleSize">Maximum number of bytes to scan.</param>
    /// <returns>True when the sample suggests the file is binary, otherwise false.</returns>
    internal static bool IsBinaryFile(byte[]? data, int sampleSize = 1024)
    {
        if (data == null || data.Length == 0)
            return false;

        var checkLength = Math.Min(sampleSize, data.Length);
        var nonPrintableCount = 0;

        for (var i = 0; i < checkLength; i++)
        {
            var currentByte = data[i];

            // Treat ASCII control characters (except common whitespace) as indicators of binary data.
            if (currentByte < 32)
            {
                if (currentByte == 9 || currentByte == 10 || currentByte == 13)
                {
                    continue;
                }

                nonPrintableCount++;
                continue;
            }

            // DEL (127) and extended ASCII bytes are typically binary.
            if (currentByte >= 127)
            {
                nonPrintableCount++;
            }
        }

        const double nonPrintableThreshold = 0.1; // 10% threshold of non-printable characters
        return (double)nonPrintableCount / checkLength > nonPrintableThreshold;
    }
}
