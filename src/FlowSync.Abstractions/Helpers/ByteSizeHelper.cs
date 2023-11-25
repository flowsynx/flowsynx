namespace FlowSync.Abstractions.Helpers;

public class ByteSizeHelper
{
    public static string FormatByteSize(long? size, bool? applyFormat = true)
    {
        return size is null ? $"{0:0.##}" : FormatByteSize(size.Value, applyFormat);
    }

    public static string FormatByteSize(long size, bool? applyFormat = true)
    {
        if (applyFormat is null or false)
            return $"{size:0.##}";

        string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };
        var order = 0;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            size /= 1024;
            order++;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}